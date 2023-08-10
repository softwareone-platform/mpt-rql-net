using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Projection;

delegate ErrorOr<Expression?> ComplexPropertyProcessor(MemberExpression member, ProjectionNode node, int depth);

internal class ProjectionService<TView> : RqlService, IProjectionService<TView>
{
    private readonly IRqlSettings _settings;
    private readonly ITypeMetadataProvider _typeMetadataProvider;
    private readonly IRqlParser _parser;

    public ProjectionService(IRqlSettings settings, ITypeMetadataProvider typeMetadataProvider, IRqlParser parser) : base(typeMetadataProvider)
    {
        _settings = settings;
        _typeMetadataProvider = typeMetadataProvider;
        _parser = parser;
    }

    protected override string ErrorPrefix => "select";

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

        var properties = _typeMetadataProvider.GetProperties(param.Type);
        var bindings = new List<MemberBinding>(properties.Count());

        var errors = new List<Error>();

        foreach (var rqlPropery in properties)
        {
            if (!rqlPropery.Flags.HasFlag(MemberFlag.Selectable))
                continue;

            if (node.TryGetChild(rqlPropery.Name, out var propertyNode))
            {
                // subtracted properties are skipped unless they have children
                if (propertyNode!.Mode == SelectMode.None && propertyNode!.Children == null)
                    continue;
            }
            // all properties are skipped if parent is in subtract mode and no there is explicit property descriptor
            else if (node.Mode == SelectMode.None)
            {
                continue;
            }

            // if parent in defaults mode skip all non reference props
            if (node.Mode == SelectMode.Defaults && !rqlPropery.Flags.HasFlag(MemberFlag.Reference) && (propertyNode == null || propertyNode.Mode != SelectMode.All))
                continue;

            var propertyInit = rqlPropery.Type switch
            {
                RqlPropertyType.Primitive => Expression.MakeMemberAccess(param, rqlPropery.Property),
                RqlPropertyType.Binary => Expression.MakeMemberAccess(param, rqlPropery.Property),
                RqlPropertyType.Reference => ProcessComplexProperty(param, propertyNode, rqlPropery, depth, ProcessReferenceProperty),
                RqlPropertyType.Collection => ProcessComplexProperty(param, propertyNode, rqlPropery, depth, ProcessCollectionProperty),
                _ => throw new NotImplementedException("Unknown RQL property type"),
            };

            if (propertyInit.IsError)
            {
                errors.AddRange(propertyInit.Errors);
                continue;
            }

            if (propertyInit.Value != null)
                bindings.Add(Expression.Bind(rqlPropery.Property, propertyInit.Value));
        }

        if (errors.Any())
            return errors;

        // produce null for non zero depth
        if (!bindings.Any() && depth != 0)
            return (MemberInitExpression?)null;

        return Expression.MemberInit(Expression.New(param.Type.GetConstructor(Type.EmptyTypes)!), bindings);
    }

    protected ErrorOr<Expression?> ProcessComplexProperty(Expression param, ProjectionNode? propertyNode, RqlPropertyInfo propertyInfo, int depth,
    ComplexPropertyProcessor processor)
    {
        propertyNode ??= new ProjectionNode { Value = propertyInfo.Name.AsMemory(), Mode = SelectMode.Defaults };

        // treat every complex property deeper than max select depth as a default reference
        if (depth >= _settings.Select.MaxDepth)
            propertyNode.Mode = SelectMode.Defaults;

        var memberAccess = Expression.MakeMemberAccess(param, propertyInfo.Property);
        return processor(memberAccess, propertyNode!, depth);
    }

    protected ErrorOr<Expression?> ProcessReferenceProperty(MemberExpression memberAccess, ProjectionNode propertyNode, int depth)
    {
        var selector = GetSelector(memberAccess, propertyNode, depth + 1);

        if (selector.IsError)
            return selector.Errors;

        if (selector.Value == null)
            return (Expression?)null;

        return Expression.Condition(
            Expression.NotEqual(memberAccess, Expression.Constant(null, memberAccess.Type)),
            selector.Value,
            Expression.Constant(null, selector.Value.Type));
    }

    protected ErrorOr<Expression?> ProcessCollectionProperty(MemberExpression memberAccess, ProjectionNode propertyNode, int depth)
    {
        var itemType = memberAccess.Type.GenericTypeArguments[0];
        var param = Expression.Parameter(itemType);
        var selector = GetSelector(param, propertyNode, depth + 1);

        if (selector.IsError)
            return selector.Errors;

        if (selector.Value == null)
            return (Expression?)null;

        var selectLambda = Expression.Lambda(selector.Value, param);

        var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<>).MakeGenericType(itemType))!;

        var selectCall = Expression.Call(null, functions.GetSelect(), memberAccess, selectLambda);
        return Expression.Call(null, functions.GetToList(), selectCall);
    }
}
