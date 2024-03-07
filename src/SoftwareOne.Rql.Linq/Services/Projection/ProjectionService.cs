using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Projection;

delegate ErrorOr<Expression?> ComplexPropertyProcessor(MemberExpression member, ProjectionNode node, int depth);

internal sealed class ProjectionService<TView> : IProjectionService<TView>
{
    private readonly IRqlSelectSettings _selectSettings;
    private readonly IMetadataProvider _typeMetadataProvider;
    private readonly IRqlParser _parser;
    private readonly IAuditContextAccessor _auditContextAccessor;
    private readonly IActionValidator _actionValidator;

    public ProjectionService(IRqlSelectSettings selectSettings, IMetadataProvider typeMetadataProvider,
        IRqlParser parser, IAuditContextAccessor auditContextAccessor, IActionValidator actionValidator)
    {
        _selectSettings = selectSettings;
        _typeMetadataProvider = typeMetadataProvider;
        _parser = parser;
        _auditContextAccessor = auditContextAccessor;
        _actionValidator = actionValidator;
    }

    public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? projection)
    {
        var node = !string.IsNullOrEmpty(projection) ? _parser.Parse(projection).ToProjection() : new();
        node.Mode = _selectSettings.Mode;

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
            if (rqlProperty.Property == null || rqlProperty.IsIgnored)
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

        if (errors.Count != 0)
            return errors;

        // produce null for non zero depth
        if (bindings.Count == 0 && depth != 0)
            return default(MemberInitExpression);

        return Expression.MemberInit(Expression.New(param.Type.GetConstructor(Type.EmptyTypes)!), bindings);
    }

    private ErrorOr<Expression?> MakePropertyInit(Expression param, ProjectionNode parentNode, RqlPropertyInfo rqlProperty, int depth)
    {
        var result = ErrorOrFactory.From<Expression?>(default);

        if (!_actionValidator.Validate(rqlProperty, RqlActions.Select))
        {
            _auditContextAccessor.ReportInvisiblePath(GetNodeFullPath);
            return result;
        }

        var (propertyNode, omitted) = GetPropertyNode(parentNode, rqlProperty);

        if (!omitted)
        {
            result = rqlProperty.Type switch
            {
                RqlPropertyType.Primitive or RqlPropertyType.Binary => MakeSimplePropertyInit(param, propertyNode, rqlProperty, depth),
                RqlPropertyType.Reference or RqlPropertyType.Collection => MakeComplexPropertyInit(param, parentNode, propertyNode, rqlProperty, depth),
                _ => throw new NotImplementedException("Unknown RQL property type"),
            };
        }

        if (omitted || result.Value == null)
        {
            _auditContextAccessor.ReportOmittedPath(GetNodeFullPath);
        }

        return result;

        string GetNodeFullPath()
        {
            var parentPath = parentNode.GetFullPath();
            return string.IsNullOrEmpty(parentPath) ? rqlProperty.Name : $"{parentPath}.{rqlProperty.Name}";
        }
    }

    private static (ProjectionNode? PropertyNode, bool IsOmitted) GetPropertyNode(ProjectionNode parentNode, RqlPropertyInfo rqlProperty)
    {
        // subtracted properties are skipped unless they have children otherwise parent mode decides
        var omitted = parentNode.TryGetChild(rqlProperty.Name, out var propertyNode)
            ? propertyNode!.Mode == RqlSelectMode.None && propertyNode.Children == null
            : parentNode.Mode == RqlSelectMode.None;

        // if parent in core mode - skip all non reference props
        if (!omitted && parentNode.Mode == RqlSelectMode.Core && !rqlProperty.IsCore && (propertyNode == null || propertyNode.Mode != RqlSelectMode.All))
            omitted = true;

        return (propertyNode, omitted);
    }

    private static ErrorOr<Expression?> MakeSimplePropertyInit(Expression param, ProjectionNode? propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        _ = propertyNode;
        _ = depth;
        return Expression.MakeMemberAccess(param, propertyInfo.Property!);
    }

    private ErrorOr<Expression?> MakeComplexPropertyInit(Expression param, ProjectionNode parentNode, ProjectionNode? propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        var undefined = propertyNode == null;
        propertyNode ??= new ProjectionNode { Value = propertyInfo.Name.AsMemory(), Mode = RqlSelectMode.Core, Parent = parentNode };

        // treat every complex property deeper than max select depth as a default reference
        if (depth >= _selectSettings.MaxDepth && propertyNode.Mode != RqlSelectMode.None)
            propertyNode.Mode = RqlSelectMode.Core;

        if (undefined)
            propertyNode.Mode = propertyInfo.SelectMode;

        return propertyInfo.Type switch
        {
            RqlPropertyType.Reference => MakeReferencePropertyInit(param, propertyNode, propertyInfo, depth),
            RqlPropertyType.Collection => MakeCollectionPropertyInit(param, propertyNode, propertyInfo, depth),
            _ => throw new NotImplementedException("Unknown RQL complex property type")
        };

    }

    private ErrorOr<Expression?> MakeReferencePropertyInit(Expression param, ProjectionNode propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        var memberAccess = Expression.MakeMemberAccess(param, propertyInfo.Property!);

        var selector = GetSelector(memberAccess, propertyNode, depth + 1);

        if (selector.IsError)
            return selector.Errors;

        if (selector.Value == null)
            return default(Expression);

        if (propertyInfo.IsNullable)
            return Expression.Condition(Expression.NotEqual(memberAccess, Expression.Constant(null, memberAccess.Type)),
                selector.Value, Expression.Constant(null, selector.Value.Type));

        return selector.Value;
    }

    private ErrorOr<Expression?> MakeCollectionPropertyInit(Expression param, ProjectionNode propertyNode, RqlPropertyInfo propertyInfo, int depth)
    {
        var memberAccess = Expression.MakeMemberAccess(param, propertyInfo.Property!);

        var itemType = memberAccess.Type.GenericTypeArguments[0];

        if (!TypeHelper.IsUserComplexType(itemType) && propertyNode.Mode != RqlSelectMode.None)
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

}
