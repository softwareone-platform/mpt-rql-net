using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Operators;
using Mpt.Rql.Services.Filtering.Operators.Collection;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Builders;

internal class CollectionExpressionBuilder : IConcreteExpressionBuilder<RqlCollection>
{
    private readonly IBuilderContext _builderContext;
    private readonly IExpressionBuilder _builder;
    private readonly IFilteringPathInfoBuilder _pathBuilder;
    private readonly IOperatorHandlerProvider _operatorHandlerProvider;

    public CollectionExpressionBuilder(IBuilderContext builderContext, IExpressionBuilder builder, IOperatorHandlerProvider operatorHandlerProvider, IFilteringPathInfoBuilder pathBuilder)
    {
        _builderContext = builderContext;
        _builder = builder;
        _pathBuilder = pathBuilder;
        _operatorHandlerProvider = operatorHandlerProvider;
    }

    public Result<Expression> Build(ParameterExpression pe, RqlCollection node)
    {
        var handler = (ICollectionOperator)_operatorHandlerProvider.GetOperatorHandler(node.GetType())!;

        var memberInfo = _pathBuilder.Build(pe, node.Left);

        if (memberInfo.IsError)
            return memberInfo.Errors;

        var property = memberInfo.Value!.PropertyInfo;
        var accessor = memberInfo.Value.Expression;

        // Struct enumerables (e.g. ImmutableArray<T>) are not reference-assignable to IEnumerable<T>; Expression.Call would throw.
        if (property.ElementType == null || accessor.Type.IsValueType)
            return Error.Validation("Collection property has incompatible type", path: _builderContext.GetFullPath(property.Name));

        var param = Expression.Parameter(property.ElementType);

        LambdaExpression? innerLambda = null;
        if (node.Right != null)
        {
            // Restore the caller's scope (not root) afterwards: this builder may run nested inside another
            // collection scope, e.g. a first() predicate, and error paths must keep that prefix.
            var previousNode = _builderContext.CurrentNode;
            _builderContext.TryGoToChild(property);
            try
            {
                var innerExpression = _builder.Build(param, node.Right);

                if (innerExpression.IsError)
                    return innerExpression.Errors;

                innerLambda = Expression.Lambda(innerExpression.Value!, param);
            }
            finally
            {
                _builderContext.SetNode(previousNode);
            }
        }

        return handler.MakeExpression(property, accessor, innerLambda);
    }
}