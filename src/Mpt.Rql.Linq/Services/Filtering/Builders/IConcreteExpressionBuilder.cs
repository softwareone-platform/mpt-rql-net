using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Builders;

internal interface IConcreteExpressionBuilder<TNode> where TNode : RqlExpression
{
    Result<Expression> Build(ParameterExpression pe, TNode node);
}
