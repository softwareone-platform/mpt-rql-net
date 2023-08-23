using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Services
{
    internal class RqlExpressionMapper
    {
        internal static RqlExpression MapFromWord(Word word)
        {
            var endIndex = word.WordStart + word.WordLength;
            // TODO: BS 22/08/2023 Will be removing this very soon
            // prop=operator=value
            if (word.Delimiters.Count == 2)
            {
                var d1 = word.Delimiters[0];
                var d2 = word.Delimiters[1];

                var exp = RqlNodeParser.Parse(word.Text[(d1 + 1)..d2].ToString(),
                    new List<ExpressionPair>()
                    {
                        new(GroupType.And, ParseArgument(Unwrap(word, word.WordStart, d1))),
                        new(GroupType.And, ParseArgument(Unwrap(word, d2+1, endIndex)))
                    });
                return exp;
            }
            // prop=value
            else if (word.Delimiters.Count == 1)
            {
                var d1 = word.Delimiters[0];

                var exp = RqlNodeParser.Parse("eq", new List<ExpressionPair>()
                    {
                         new(GroupType.And, ParseArgument(Unwrap(word, word.WordStart, d1))),
                         new(GroupType.And, ParseArgument(Unwrap(word, d1 + 1, endIndex)))
                    });
                return exp;
            }
            else
            {
                return ParseArgument(Unwrap(word, word.WordStart, endIndex));
            }

            static string Unwrap(Word word, int from, int to)
            {
                if (word.WrapStart.HasValue)
                {
                    var wrapStart = word.WrapStart.Value;
                    var wrapEnd = word.WrapEnd ?? word.Text.Length - 1;

                    from = wrapStart >= from && wrapEnd < to ? wrapStart + 1 : from;
                    to = wrapEnd < to ? wrapEnd : to;
                }
                return word.Text[from..to].ToString();
            }
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
