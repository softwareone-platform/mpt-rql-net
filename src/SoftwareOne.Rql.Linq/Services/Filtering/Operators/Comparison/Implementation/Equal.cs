using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;

internal class Equal : ComparisonOperator, IEqual
{
    protected override RqlOperators Operator => RqlOperators.Eq;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.Equal;
}