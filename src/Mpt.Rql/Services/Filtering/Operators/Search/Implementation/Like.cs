using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using Mpt.Rql.Linq.Core.Expressions;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Linq.Services.Filtering.Operators.Search.Implementation;

internal class Like : ILike
{
    private static readonly MethodInfo _methodStartsWith = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo _methodEndsWith = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
    private static readonly MethodInfo _methodContains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo _methodEquals = typeof(string).GetMethod(nameof(string.Equals), [typeof(string)])!;
    private static readonly char _escapeCharacter = '\\'; 
    private static readonly char _wildcard = '*';
    private static readonly string _escapedWildcard = $"{_escapeCharacter}{_wildcard}";

    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string pattern)
    {
        var (startsWithWildCard, startsWithEscapedWildCard, endsWithEscapedWildCard, endsWithWildCard) = ResolveWildCardFacts(pattern);
        var (methodInfo, rqlOperator) = ResolveMethodInfoAndRqlOperator(pattern, startsWithWildCard, endsWithWildCard);

        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, rqlOperator);
        if (validationResult.IsError) return validationResult.Errors;

        var cleanedString = CleanToLiteralSearchString(pattern, startsWithWildCard, startsWithEscapedWildCard, endsWithEscapedWildCard, endsWithWildCard);
        return Expression.Call(member, methodInfo, ConstantBuilder.Build(cleanedString, typeof(string)));
    }

    protected virtual bool IsInsensitive => false;

    private static (bool, bool, bool, bool) ResolveWildCardFacts(string pattern)
    {
        var startsWithWildCard = pattern.StartsWith(_wildcard);
        var startsWithEscapedWildCard = pattern.StartsWith(_escapedWildcard);
        var endsWithEscapedWildCard = pattern.EndsWith(_escapedWildcard);
        var endsWithWildCard = !endsWithEscapedWildCard && pattern.EndsWith(_wildcard);

        return (startsWithWildCard, startsWithEscapedWildCard, endsWithEscapedWildCard, endsWithWildCard);
    }

    private static (MethodInfo, RqlOperators) ResolveMethodInfoAndRqlOperator(string pattern, bool startsWithWildCard, bool endsWithWildCard)
    {
        return pattern switch
        {
            var _ when startsWithWildCard && endsWithWildCard => (_methodContains, RqlOperators.Contains),
            var _ when startsWithWildCard => (_methodEndsWith, RqlOperators.EndsWith),
            var _ when endsWithWildCard => (_methodStartsWith, RqlOperators.StartsWith),
            _ => (_methodEquals, RqlOperators.Eq)
        };
    }

    private static string CleanToLiteralSearchString(string pattern, bool startsWithWildCard, bool startsWithEscapedWildCard,
        bool endsWithEscapedWildCard, bool endsWithWildCard)
    {
        if (startsWithWildCard) pattern = pattern[1..];
        else if (startsWithEscapedWildCard) pattern = pattern[1..];

        if (endsWithEscapedWildCard) pattern = $"{pattern[..^2]}{_wildcard}";
        else if (endsWithWildCard) pattern = pattern[..^1];

        return pattern;
    }
}