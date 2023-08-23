using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Services
{
    public class RqlParser : IRqlParser
    {
        private static readonly HashSet<char> _textDelimiters;
        private static readonly Dictionary<char, GroupType> _operatorToType = new()
        {
            { ',' , GroupType.And },
            { '&' , GroupType.And },
            { ';' , GroupType.Or },
            { '|' , GroupType.Or }
        };

        static RqlParser()
        {
            _textDelimiters = new HashSet<char> { '"', '\'' };
        }

        public RqlGroup Parse(string expression)
        {
            var exp = RqlExpressionReducer.Reduce(ParseInternal(expression.AsMemory(), 0, out int _));
            if (exp is RqlGroup grp)
                return grp;
            return RqlExpression.Group("", exp);
        }

        private static IList<ExpressionPair> ParseInternal(ReadOnlyMemory<char> query, int startIndex, out int x, params char[] endChars)
        {
            x = startIndex;
            var expressions = new List<ExpressionPair>();

            char c;
            var word = Word.Make(query, startIndex);
            var querySpan = query.Span;

            while (x < query.Length)
            {
                c = querySpan[x];
                if (c == '=')
                    word.Delimiters.Add(x);

                // end of delimiter
                if (word.WrapSymbol != null)
                {
                    if (c == word.WrapSymbol)
                        word.WrapEnd = x;

                    word.WordLength++;
                }
                // start of delimiter
                else if (_textDelimiters.Contains(c))
                {
                    word.WrapSymbol = c;
                    word.WrapStart = x;
                    word.WordLength++;
                }
                else if (_operatorToType.TryGetValue(c, out var nextType))
                {
                    if (word.WordLength > 0)
                    {
                        var exp = RqlExpressionMapper.MapFromWord(word);
                        expressions.Add(new ExpressionPair(word.GroupType, exp));
                    }

                    word = Word.Make(query, x + 1);
                    word.GroupType = nextType;
                }
                else if (c == '(')
                {
                    // handle functions as a single word
                    if (querySpan[x + 1] == ')')
                    {
                        x += 2;
                        word.WordLength += 2;
                        continue;
                    }

                    var innerNodes = ParseInternal(query, x + 1, out x, ')');
                    var node = RqlNodeParser.Parse(word.ToString(), innerNodes);
                    expressions.Add(new ExpressionPair(word.GroupType, node));

                    word = Word.Make(query, x + 1);
                    continue;
                }
                else if (endChars != null && endChars.Contains(c))
                {
                    x++;
                    break;
                }
                else
                    word.WordLength++;

                x++;
            };

            if (word.WordLength > 0)
                expressions.Add(new ExpressionPair(word.GroupType, RqlExpressionMapper.MapFromWord(word)));

            return expressions;
        }
    }
}