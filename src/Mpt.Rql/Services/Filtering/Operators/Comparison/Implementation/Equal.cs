using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

internal class Equal(IRqlSettings settings) : ComparisonOperator(settings), IEqual
{
    private readonly IRqlSettings _settings = settings;

    protected override RqlOperators Operator => RqlOperators.Eq;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.Equal;

    protected override bool AvoidLexicographicalComparison => true;

    protected override Result<Expression> MakeBinaryExpression(Expression accessor, string? value)
    {
        if (accessor.Type == typeof(string) && value != null)
            return accessor.Equals(value, _settings);

        return base.MakeBinaryExpression(accessor, value);
    }
}