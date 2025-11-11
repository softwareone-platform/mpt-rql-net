using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Unary;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Filtering;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Builders;

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