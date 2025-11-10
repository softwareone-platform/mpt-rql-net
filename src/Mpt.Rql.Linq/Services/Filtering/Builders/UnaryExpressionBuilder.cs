using Mpt.Rql.Abstractions.Unary;
using Mpt.Rql.Linq.Core;
using Mpt.Rql.Linq.Services.Filtering.Operators;
using Mpt.Rql.Linq.Services.Filtering.Operators.Unary;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Builders;

internal class UnaryExpressionBuilder : IConcreteExpressionBuilder<RqlUnary>
{
    private readonly IExpressionBuilder _builder;
    private readonly IOperatorHandlerProvider _operatorHandlerProvider;

    public UnaryExpressionBuilder(IExpressionBuilder builder, IOperatorHandlerProvider operatorHandlerProvider)
    {
        _builder = builder;
        _operatorHandlerProvider = operatorHandlerProvider;
    }

    public Result<Expression> Build(ParameterExpression pe, RqlUnary node)
    {
        var handler = (IUnaryOperator)_operatorHandlerProvider.GetOperatorHandler(node.GetType())!;
        var expression = _builder.Build(pe, node.Nested);
        return expression.IsError ? expression.Errors : handler.MakeExpression(expression.Value!);
    }
}