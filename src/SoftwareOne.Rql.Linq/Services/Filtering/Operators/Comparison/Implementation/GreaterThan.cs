using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class GreaterThan : ComparisonOperator, IGreaterThan
{
    protected override RqlOperators Operator => RqlOperators.Gt;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.GreaterThan;
}