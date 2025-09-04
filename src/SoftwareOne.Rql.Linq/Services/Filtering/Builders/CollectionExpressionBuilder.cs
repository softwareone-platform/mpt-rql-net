using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Abstractions.Result;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

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

        if (accessor is not MemberExpression member)
            return Error.General("Collection operations work with properties only");

        if (property.ElementType == null)
            return Error.General("Collection property has incompatible type");

        var param = Expression.Parameter(property.ElementType);

        LambdaExpression? innerLambda = null;
        if (node.Right != null)
        {
            _builderContext.TryGoToChild(property);
            var innerExpression = _builder.Build(param, node.Right);

            if (innerExpression.IsError)
                return innerExpression.Errors;

            innerLambda = Expression.Lambda(innerExpression.Value!, param);
            _builderContext.GoToRoot();
        }

        return handler.MakeExpression(property, member, innerLambda);
    }
}