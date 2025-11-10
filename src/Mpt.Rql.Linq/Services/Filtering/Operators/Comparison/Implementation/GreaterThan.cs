using Mpt.Rql.Abstractions.Configuration;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class GreaterThan(IRqlSettings settings) : ComparisonOperator(settings), IGreaterThan
{
    protected override RqlOperators Operator => RqlOperators.Gt;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.GreaterThan;
}