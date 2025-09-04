using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Abstractions.Result;
using SoftwareOne.Rql.Abstractions.Unary;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

internal class ExpressionBuilder : IExpressionBuilder
{
    private readonly IServiceProvider _serviceProvider;

    public ExpressionBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Result<Expression> Build(ParameterExpression pe, RqlExpression node)
    {
        return node switch
        {
            RqlGroup group => Build(pe, group),
            RqlBinary binary => Build(pe, binary),
            RqlUnary unary => Build(pe, unary),
            RqlCollection collection => Build(pe, collection),
            _ => FilteringError.Internal
        };

        Result<Expression> Build<TNode>(ParameterExpression parameter, TNode node) where TNode : RqlExpression
            => _serviceProvider.GetRequiredService<IConcreteExpressionBuilder<TNode>>().Build(parameter, node);
    }
}