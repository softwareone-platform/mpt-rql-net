using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Projection;

delegate ErrorOr<Expression?> ComplexPropertyProcessor(MemberExpression member, ProjectionNode node, int depth);

internal class ProjectionService<TView> : IProjectionService<TView>
{
    private readonly IRqlSettings _settings;
    private readonly IMetadataProvider _typeMetadataProvider;
    private readonly IRqlParser _parser;

    public ProjectionService(IRqlSettings settings, IMetadataProvider typeMetadataProvider, IRqlParser parser)
    {
        _settings = settings;
        _typeMetadataProvider = typeMetadataProvider;
        _parser = parser;
    }

    public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? projection)
    {
        var node = !string.IsNullOrEmpty(projection) ? _parser.Parse(projection).ToProjection() : new();
        node.Mode = _settings.Select.Mode;

        var param = Expression.Parameter(typeof(TView));
        var selector = GetSelector(param, node, 0);

        if (selector.IsError)
            return selector.Errors;

        return ErrorOrFactory.From(query.Select((Expression<Func<TView, TView>>)Expression.Lambda(selector.Value!, param)));
    }

    private ErrorOr<MemberInitExpression?> GetSelector(Expression param, ProjectionNode node, int depth)
    {
        if (depth > 100)
            return Error.Validation(node.Value.ToString(), "Extreme select depth detected. Most likely a circular dependency issue.");

        var properties = _typeMetadataProvider.GetPropertiesByDeclaringType(param.Type);
        var bindings = new List<MemberBinding>(properties.Count());

        var errors = new List<Error>();

        foreach (var rqlProperty in properties)
        {
            if (rqlProperty.Property == null)
                continue;

            var propertyInit = MakePropertyInit(param, node, rqlProperty, depth);

            if (propertyInit.IsError)
            {
                errors.AddRange(propertyInit.Errors);
                continue;
            }

            if (propertyInit.Value != null)
                bindings.Add(Expression.Bind(rqlProperty.Property, propertyInit.Value));
        }

        if (errors.Any())
            return errors;

        // produce null for non zero depth
        if (!bindings.Any() && depth != 0)
            return default(MemberInitExpression);

        return Expression.MemberInit(Expression.New(param.Type.GetConstructor(Type.EmptyTypes)!), bindings);
    }

    protected ErrorOr<Expression?> MakePropertyInit(Expression param, ProjectionNode parentNode, RqlPropertyInfo rqlProperty, int depth)
    {
        var result = ErrorOrFactory.From<Expression?>(default);

        if (!rqlProperty.Actions.HasFlag(RqlActions.Select))
            return result;

        if (parentNode.TryGetChild(rqlProperty.Name, out var propertyNode))
        {
            // subtracted properties are skipped unless they have children
            if (propertyNode!.Mode == SelectMode.None && propertyNode!.Children == null)
                return result;
        }
        // hidden properties are ignored unless requested explicitly
        else if (rqlProperty.IsHidden)
            return result;

        // all properties are skipped if parent is in subtract mode and no there is explicit property descriptor
        else if (parentNode.Mode == SelectMode.None)
            return result;

        // if parent in defaults mode skip all non reference props
        if (parentNode.Mode == SelectMode.Core && !rqlProperty.IsCore && (propertyNode == null || propertyNode.Mode != SelectMode.All))
            return result;

        result = rqlProperty.Type switch
        {
            RqlPropertyType.Primitive => MakeSimplePropertyInit(param, propertyNode, rqlProperty, depth),
            RqlPropertyType.Binary => MakeSimplePropertyInit(param, propertyNode, rqlProperty, depth),
            RqlPropertyType.Reference => MakeReferencePropertyInit(param, propertyNode, rqlProperty, depth),
            RqlPropertyType.Collection => MakeCollectionPropertyInit(param, propertyNode, rqlProperty, depth),
            _ => throw new NotImplementedException("Unknown RQL property type"),
        };

        return result;
    }

    protected static ErrorOr<Expression?> MakeSimplePropertyInit(Expression param, ProjectionNode? propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        _ = propertyNode;
        _ = depth;
        return Expression.MakeMemberAccess(param, propertyInfo.Property!);
    }

    protected ErrorOr<Expression?> MakeReferencePropertyInit(Expression param, ProjectionNode? propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        propertyNode = EnsureComplexPropertyNode(propertyNode, propertyInfo, depth);
        var memberAccess = Expression.MakeMemberAccess(param, propertyInfo.Property!);

        var selector = GetSelector(memberAccess, propertyNode, depth + 1);

        if (selector.IsError)
            return selector.Errors;

        if (selector.Value == null)
            return default(Expression);

        return Expression.Condition(
            Expression.NotEqual(memberAccess, Expression.Constant(null, memberAccess.Type)),
            selector.Value,
            Expression.Constant(null, selector.Value.Type));
    }

    protected ErrorOr<Expression?> MakeCollectionPropertyInit(Expression param, ProjectionNode? propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        propertyNode = EnsureComplexPropertyNode(propertyNode, propertyInfo, depth);
        var memberAccess = Expression.MakeMemberAccess(param, propertyInfo.Property!);

        var itemType = memberAccess.Type.GenericTypeArguments[0];

        if (!TypeHelper.IsUserComplexType(itemType))
            return memberAccess;

        var innerParam = Expression.Parameter(itemType);
        var selector = GetSelector(innerParam, propertyNode, depth + 1);

        if (selector.IsError)
            return selector.Errors;

        if (selector.Value == null)
            return default(Expression);

        var selectLambda = Expression.Lambda(selector.Value, innerParam);

        var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<>).MakeGenericType(itemType))!;

        var selectCall = Expression.Call(null, functions.GetSelect(), memberAccess, selectLambda);
        return Expression.Call(null, functions.GetToList(), selectCall);
    }

    protected ProjectionNode EnsureComplexPropertyNode(ProjectionNode? propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        propertyNode ??= new ProjectionNode { Value = propertyInfo.Name.AsMemory(), Mode = SelectMode.Core };

        // treat every complex property deeper than max select depth as a default reference
        if (depth >= _settings.Select.MaxDepth)
            propertyNode.Mode = SelectMode.Core;

        return propertyNode;
    }
}
