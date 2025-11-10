using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Builders;

internal interface IExpressionBuilder
{
    Result<Expression> Build(ParameterExpression pe, RqlExpression node);
}
