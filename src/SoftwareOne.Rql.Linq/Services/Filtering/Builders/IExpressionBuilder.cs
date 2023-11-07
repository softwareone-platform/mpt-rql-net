using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

internal interface IExpressionBuilder
{
    ErrorOr<Expression> Build(ParameterExpression pe, RqlExpression node);
}
