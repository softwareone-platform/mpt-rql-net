using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

internal class NotEqual(IRqlSettings settings) : ComparisonOperator(settings), INotEqual
{
    private readonly IRqlSettings _settings = settings;

    protected override RqlOperators Operator => RqlOperators.Ne;

    internal override Func<Expression, Expression, BinaryExpression> Handler => Expression.NotEqual;

    protected override bool AvoidLexicographicalComparison => true;

    protected override Result<Expression> MakeBinaryExpression(Expression accessor, string? value)
    {
        if (accessor.Type == typeof(string) && value != null)
            return StringExpressionHelper.NotEquals(accessor, value, _settings.Filter.Strings.Comparison);

        return base.MakeBinaryExpression(accessor, value);
    }
}