using SoftwareOne.Rql.Linq.Configuration;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class Equal(IRqlSettings settings) : ComparisonOperator(settings), IEqual
{
    protected override RqlOperators Operator => RqlOperators.Eq;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.Equal;

    protected override bool AvoidLexicographicalComparison => true;
}