using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class NotEqual : ComparisonOperator, INotEqual
{
    public ErrorOr<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string? value)
        => MakeBinaryExpression(propertyInfo, member, value, Expression.NotEqual);

    protected override RqlOperators Operator => RqlOperators.Ne;
}