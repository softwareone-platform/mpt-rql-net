using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class GreaterThan : ComparisonOperator, IGreaterThan
{
    public ErrorOr<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string? value)
        => MakeBinaryExpression(propertyInfo, member, value, Expression.GreaterThan);

    protected override RqlOperators Operator => RqlOperators.Gt;
}