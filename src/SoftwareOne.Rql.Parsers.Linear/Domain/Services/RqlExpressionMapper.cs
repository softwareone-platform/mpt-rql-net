using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Services;

internal static class RqlExpressionMapper
{
    internal static RqlExpression MapFromWord(Word word)
    {
        var endIndex = word.WordStart + word.WordLength;
        return RqlExpression.Constant(Unwrap(word, word.WordStart, endIndex));
    }

    private static string Unwrap(Word word, int fromIndex, int toIndex)
    {
        if (word.WrapStart.HasValue)
        {
            var wrapStart = word.WrapStart.Value;
            var wrapEnd = word.WrapEnd ?? word.Text.Length - 1;

            fromIndex = wrapStart >= fromIndex && wrapEnd < toIndex ? wrapStart + 1 : fromIndex;
            toIndex = wrapEnd < toIndex ? wrapEnd : toIndex;
        }
        return word.Text[fromIndex..toIndex].ToString();
    }
}
