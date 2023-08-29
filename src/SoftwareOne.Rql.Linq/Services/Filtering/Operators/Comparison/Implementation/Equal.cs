using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class Equal : ComparisonOperator, IEqual
{
    public ErrorOr<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string? value)
        => MakeBinaryExpression(propertyInfo, member, value, Expression.Equal);

    protected override RqlOperators Operator => RqlOperators.Eq;
}