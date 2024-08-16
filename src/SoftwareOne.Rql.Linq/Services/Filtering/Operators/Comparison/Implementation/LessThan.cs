using SoftwareOne.Rql.Linq.Configuration;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class LessThan(IRqlSettings settings) : ComparisonOperator(settings), ILessThan
{
    protected override RqlOperators Operator => RqlOperators.Lt;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.LessThan;
}
