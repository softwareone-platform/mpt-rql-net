using ErrorOr;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class LessThan : ComparisonOperator, ILessThan
{
    public ErrorOr<Expression> MakeExpression(MemberExpression member, string? value)
        => MakeBinaryExpression(member, value, Expression.LessThan);
}
