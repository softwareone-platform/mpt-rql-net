using Mpt.Rql.Abstractions.Configuration;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class LessThanOrEqual(IRqlSettings settings) : ComparisonOperator(settings), ILessThanOrEqual
{
    protected override RqlOperators Operator => RqlOperators.Le;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.LessThanOrEqual;
}