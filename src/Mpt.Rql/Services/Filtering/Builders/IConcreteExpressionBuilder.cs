using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Builders;

internal interface IConcreteExpressionBuilder<TNode> where TNode : RqlExpression
{
    Result<Expression> Build(ParameterExpression pe, TNode node);
}
