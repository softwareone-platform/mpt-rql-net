using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Builders;

internal interface IExpressionBuilder
{
    Result<Expression> Build(ParameterExpression pe, RqlExpression node);
}
