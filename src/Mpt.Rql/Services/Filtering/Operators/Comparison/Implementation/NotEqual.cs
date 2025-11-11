using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Services.Filtering.Operators.Comparison;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

internal class NotEqual(IRqlSettings settings) : ComparisonOperator(settings), INotEqual
{
    protected override RqlOperators Operator => RqlOperators.Ne;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.NotEqual;

    protected override bool AvoidLexicographicalComparison => true;
}