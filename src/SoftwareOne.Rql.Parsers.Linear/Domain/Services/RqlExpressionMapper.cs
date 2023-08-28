using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Exception;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Services
{
    internal class RqlExpressionMapper
    {
        internal static RqlExpression MapFromWord(Word word)
        {
            if (word.Delimiters.Count > 1)
                throw new RqlExpressionMapperException("Mapping from a word can only have zero or one delimiter");
            
            var endIndex = word.WordStart + word.WordLength;

            if (word.Delimiters.Count == 1)
                return RqlNodeParser.Parse(Constants.RqlTerm.Equal, new List<ExpressionPair>()
                    {
                         new(GroupType.And, ParseArgument(Unwrap(word, word.WordStart, word.Delimiters[0]))),
                         new(GroupType.And, ParseArgument(Unwrap(word, word.Delimiters[0] + 1, endIndex)))
                    });
           
            return ParseArgument(Unwrap(word, word.WordStart, endIndex));
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

        private static RqlArgument ParseArgument(string text)
        {
            return text switch
            {
                "null()" => RqlExpression.Null(),
                "empty()" => RqlExpression.Empty(),
                _ => RqlExpression.Constant(text)
            };
        }
    }
}
