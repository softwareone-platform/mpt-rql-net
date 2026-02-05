using Mpt.Rql.Abstractions;
using Mpt.Rql.Parsers.Linear.Core.ValueTypes;

namespace Mpt.Rql.Parsers.Linear.Services;

internal static class RqlExpressionMapper
{
    internal static RqlExpression MapFromWord(Word word)
    {
        var endIndex = word.WordStart + word.WordLength;
        return RqlExpression.Constant(RemoveQuotes(word, word.WordStart, endIndex), word.IsQuoted);
    }

    private static string RemoveQuotes(Word word, int fromIndex, int toIndex)
    {
        if (word.QuoteStart.HasValue)
        {
            var quoteStart = word.QuoteStart.Value;
            var quoteEnd = word.QuoteEnd ?? word.Text.Length - 1;

            fromIndex = quoteStart >= fromIndex && quoteEnd < toIndex ? quoteStart + 1 : fromIndex;
            toIndex = quoteEnd < toIndex ? quoteEnd : toIndex;
        }
        return word.Text[fromIndex..toIndex].ToString();
    }
}
