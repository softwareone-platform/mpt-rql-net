using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Services.Filtering.Operators.Comparison;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

internal class LessThan(IRqlSettings settings) : ComparisonOperator(settings), ILessThan
{
    protected override RqlOperators Operator => RqlOperators.Lt;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.LessThan;
}
