using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Projection
{
    delegate ErrorOr<Expression?> ComplexPropertyProcessor(MemberExpression member, ProjectionNode node, int depth);

    internal class ProjectionService<TView> : RqlService, IProjectionService<TView>
    {
        private readonly ITypeMetadataProvider _typeNameMaper;
        private readonly IRqlParser _parser;

        public ProjectionService(ITypeMetadataProvider typeNameMaper, IRqlParser parser) : base(typeNameMaper)
        {
            _typeNameMaper = typeNameMaper;
            _parser = parser;
        }

        protected override string ErrorPrefix => "select";

        public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? projection)
        {
            ProjectionNode node;
            if (!string.IsNullOrEmpty(projection))
            {
                var rqlNode = _parser.Parse(projection);
                node = ProjectionNodeBuilder.Build(rqlNode);
            }
            else
            {
                node = MakeDefaultProjectionNode();
            }

            var param = Expression.Parameter(typeof(TView));
            var eoSelector = GetSelector(param, node, 0);

            if (eoSelector.IsError)
                return eoSelector.Errors;

            return ErrorOrFactory.From(query.Select((Expression<Func<TView, TView>>)Expression.Lambda(eoSelector.Value, param)));
        }

        private ErrorOr<MemberInitExpression> GetSelector(Expression param, ProjectionNode node, int depth)
        {
            if (node.Type == ProjectionNodeType.Value)
                return Error.Validation(MakeErrorCode(node.Value.ToString()), 
                    $"Simple property was not expected. Are you missing select statement? Example: {node.Value}(*).");

            var properties = _typeNameMaper.ListProperties(param.Type);
            var bindings = new List<MemberBinding>(properties.Count);

            var errors = new List<Error>();

            foreach (var item in properties)
            {
                var propertyWrapper = item.Value;
                var property = propertyWrapper.Property;

                ProjectionNode? propertyNode = null;
                if (node.Children != null && node.Children.TryGetValue(item.Key, out propertyNode))
                {
                    // property subtracted explicitly
                    if (!propertyNode!.Sign)
                        continue;
                }
                else
                {
                    // current node is non default
                    if (node.Type == ProjectionNodeType.None)
                        continue;

                    // or else property is not a default
                    else if (!item.Value.Flags.HasFlag(MemberFlag.IsDefault))
                        continue;
                }

                var memberAccess = Expression.MakeMemberAccess(param, property);

                var eoPropertyInit = propertyWrapper.Type switch
                {
                    RqlPropertyType.Primitive => memberAccess,
                    RqlPropertyType.Binary => memberAccess,
                    RqlPropertyType.Reference => ProcessComplexProperty(memberAccess, propertyNode, depth, ProcessReferenceProperty),
                    RqlPropertyType.Collection => ProcessComplexProperty(memberAccess, propertyNode, depth, ProcessCollectionProperty),
                    _ => throw new NotImplementedException("Unknown RQL property type"),
                };


                if (eoPropertyInit.IsError)
                    errors.AddRange(eoPropertyInit.Errors);

                if (eoPropertyInit.Value == null)
                    continue;

                bindings.Add(Expression.Bind(property, eoPropertyInit.Value));
            }

            if (errors.Any())
                return errors;

            return Expression.MemberInit(Expression.New(param.Type.GetConstructor(Type.EmptyTypes)!), bindings);
        }

        protected static ErrorOr<Expression?> ProcessComplexProperty(MemberExpression memberAccess, ProjectionNode? propertyNode, int depth,
            ComplexPropertyProcessor processor)
        {
            if (propertyNode == null)
            {
                if (depth == 0)
                    propertyNode = MakeDefaultProjectionNode();
                else
                    return (Expression?)null;
            }

            return processor(memberAccess, propertyNode, depth);
        }

        protected ErrorOr<Expression?> ProcessReferenceProperty(MemberExpression memberAccess, ProjectionNode propertyNode, int depth)
        {
            var selector = GetSelector(memberAccess, propertyNode, depth + 1);

            if (selector.IsError)
                return selector.Errors;

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

            var selectLambda = Expression.Lambda(selector.Value, param);

            var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<>).MakeGenericType(itemType))!;

            var selectCall = Expression.Call(null, functions.GetSelect(), memberAccess, selectLambda);
            return Expression.Call(null, functions.GetToList(), selectCall);
        }

        private static ProjectionNode MakeDefaultProjectionNode() => new() { Type = ProjectionNodeType.Defaults };
    }
}
