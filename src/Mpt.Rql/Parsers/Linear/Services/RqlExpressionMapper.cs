using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Exception;
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
            var delimiter = word.Text.Span[quoteStart];
            var isOpen = word.QuoteSymbol != null;

            // an open quote runs to the end of the query and any delimiter in it is half of an escaped pair,
            // so finding one means an escape left the value unterminated rather than a plain missing quote
            if (isOpen && word.Text.Span[(quoteStart + 1)..].Contains(delimiter))
            {
                throw new RqlParserException("Unterminated quoted value");
            }

            var quoteEnd = word.QuoteEnd ?? word.Text.Length - 1;

            fromIndex = quoteStart >= fromIndex && quoteEnd < toIndex ? quoteStart + 1 : fromIndex;
            toIndex = quoteEnd < toIndex ? quoteEnd : toIndex;

            var value = word.Text[fromIndex..toIndex].ToString();

            // a closed value can hold its delimiter only as escaped pairs, so without one this leaves the value as it is
            return isOpen ? value : value.Replace(new string(delimiter, 2), delimiter.ToString());
        }
        return word.Text[fromIndex..toIndex].ToString();
    }
}
