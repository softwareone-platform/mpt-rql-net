using SoftwareOne.Rql.Linq.Configuration;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class NotEqual(IRqlSettings settings) : ComparisonOperator(settings), INotEqual
{
    protected override RqlOperators Operator => RqlOperators.Ne;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.NotEqual;

    protected override bool AvoidLexicographicalComparison => true;
}