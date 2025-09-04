using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Result;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

internal interface IExpressionBuilder
{
    Result<Expression> Build(ParameterExpression pe, RqlExpression node);
}
