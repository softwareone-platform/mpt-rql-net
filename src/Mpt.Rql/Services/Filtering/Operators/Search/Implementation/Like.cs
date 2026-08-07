using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Operators.Search.Implementation;

internal class Like(IRqlSettings settings) : ILike
{
    private static readonly char _escapeCharacter = '\\';
    private static readonly char _wildcard = '*';
    private static readonly string _escapedWildcard = $"{_escapeCharacter}{_wildcard}";

    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string pattern)
    {
        var (startsWithWildCard, startsWithEscapedWildCard, endsWithEscapedWildCard, endsWithWildCard) = ResolveWildCardFacts(pattern);
        var rqlOperator = ResolveRqlOperator(pattern, startsWithWildCard, endsWithWildCard);

        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, rqlOperator);
        if (validationResult.IsError) return validationResult.Errors;

        var cleanedString = CleanToLiteralSearchString(pattern, startsWithWildCard, startsWithEscapedWildCard, endsWithEscapedWildCard, endsWithWildCard);

        return rqlOperator switch
        {
            RqlOperators.Contains => accessor.Contains(cleanedString, settings),
            RqlOperators.EndsWith => accessor.EndsWith(cleanedString, settings),
            RqlOperators.StartsWith => accessor.StartsWith(cleanedString, settings),
            RqlOperators.Eq => accessor.Equals(cleanedString, settings),
            _ => throw new InvalidOperationException($"Unsupported operator: {rqlOperator}")
        };
    }

    private static (bool, bool, bool, bool) ResolveWildCardFacts(string pattern)
    {
        var startsWithWildCard = pattern.StartsWith(_wildcard);
        var startsWithEscapedWildCard = pattern.StartsWith(_escapedWildcard);
        var endsWithEscapedWildCard = pattern.EndsWith(_escapedWildcard);
        var endsWithWildCard = !endsWithEscapedWildCard && pattern.EndsWith(_wildcard);

        return (startsWithWildCard, startsWithEscapedWildCard, endsWithEscapedWildCard, endsWithWildCard);
    }

    private static RqlOperators ResolveRqlOperator(string pattern, bool startsWithWildCard, bool endsWithWildCard)
    {
        return pattern switch
        {
            var _ when startsWithWildCard && endsWithWildCard => RqlOperators.Contains,
            var _ when startsWithWildCard => RqlOperators.EndsWith,
            var _ when endsWithWildCard => RqlOperators.StartsWith,
            _ => RqlOperators.Eq
        };
    }

    private static string CleanToLiteralSearchString(string pattern, bool startsWithWildCard, bool startsWithEscapedWildCard,
        bool endsWithEscapedWildCard, bool endsWithWildCard)
    {
        var start = startsWithWildCard || startsWithEscapedWildCard ? 1 : 0;
        var end = pattern.Length;

        if (endsWithEscapedWildCard) end = Math.Max(start, end - 2);
        else if (endsWithWildCard) end = Math.Max(start, end - 1);

        var literal = pattern[start..end];

        return endsWithEscapedWildCard ? $"{literal}{_wildcard}" : literal;
    }
}