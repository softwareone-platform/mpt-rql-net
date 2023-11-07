using ErrorOr;
using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

internal class CollectionExpressionBuilder : IConcreteExpressionBuilder<RqlCollection>
{
    private readonly IExpressionBuilder _builder;
    private readonly IFilteringPathInfoBuilder _pathBuilder;
    private readonly IOperatorHandlerProvider _operatorHandlerProvider;

    public CollectionExpressionBuilder(IExpressionBuilder builder, IOperatorHandlerProvider operatorHandlerProvider, IFilteringPathInfoBuilder pathBuilder)
    {
        _builder = builder;
        _pathBuilder = pathBuilder;
        _operatorHandlerProvider = operatorHandlerProvider;
    }

    public ErrorOr<Expression> Build(ParameterExpression pe, RqlCollection node)
    {
        var handler = (ICollectionOperator)_operatorHandlerProvider.GetOperatorHandler(node.GetType())!;

        var memberInfo = _pathBuilder.Build(pe, node.Left);

        if (memberInfo.IsError)
            return memberInfo.Errors;

        var property = memberInfo.Value.PropertyInfo;
        var accessor = memberInfo.Value.Expression;

        if (accessor is not MemberExpression member)
            return Error.Failure(description: "Collection operations work with properties only");

        if (property.ElementType == null)
            return Error.Failure(description: "Collection property has incompatible type");

        var param = Expression.Parameter(property.ElementType);

        LambdaExpression? innerLambda = null;
        if (node.Right != null)
        {
            var innerExpression = _builder.Build(param, node.Right);

            if (innerExpression.IsError)
                return innerExpression.Errors;

            innerLambda = Expression.Lambda(innerExpression.Value, param);
        }

        return handler.MakeExpression(property, member, innerLambda);
    }
}