using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Abstractions.Unary;
using System.Globalization;

namespace SoftwareOne.Rql.Parsers.Linear
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
            var exp = Reduce(ParseInternal(expression.AsMemory(), 0, out int _));
            if (exp is RqlGroup grp)
                return grp;
            return RqlExpression.Group("", exp);
        }

        private static IList<ExpressionPair> ParseInternal(ReadOnlyMemory<char> query, int startIndex, out int x, params char[] endChars)
        {
            // var words = new List<Word>();
            x = startIndex;
            var expressions = new List<ExpressionPair>();

            char c;
            var word = Word.Make(query, startIndex);
            var querySpan = query.Span;

            while (x < query.Length)
            {
                c = querySpan[x];
                if (c == '=')
                    word.Delimeters.Add(x);

                // end of delimeter
                if (word.WrapSymbol != null)
                {
                    if (c == word.WrapSymbol)
                        word.WrapEnd = x;

                    word.WordLenght++;
                }
                // start of delimeter
                else if (_textDelimiters.Contains(c))
                {
                    word.WrapSymbol = c;
                    word.WrapStart = x;
                    word.WordLenght++;
                }
                else if (_operatorToType.TryGetValue(c, out var nextType))
                {
                    if (word.WordLenght > 0)
                    {
                        var exp = WordToExpression(word);
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
                        word.WordLenght += 2;
                        continue;
                    }

                    var innerNodes = ParseInternal(query, x + 1, out x, ')');
                    var node = ParseNode(word.ToString(), innerNodes);
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
                    word.WordLenght++;

                x++;
            };


            if (word.WordLenght > 0)
                expressions.Add(new ExpressionPair(word.GroupType, WordToExpression(word)));

            return expressions;
        }

        private static RqlExpression WordToExpression(Word word)
        {
            var endIndex = word.WordStart + word.WordLenght;
            // prop=operator=value
            if (word.Delimeters.Count == 2)
            {
                var d1 = word.Delimeters[0];
                var d2 = word.Delimeters[1];

                var exp = ParseNode(word.Text[(d1 + 1)..d2].ToString(),
                    new List<ExpressionPair>()
                    {
                        new(GroupType.And, ParseArgument(Unwrap(word, word.WordStart, d1))),
                        new(GroupType.And, ParseArgument(Unwrap(word, d2+1, endIndex)))
                    });
                return exp;
            }
            // prop=value
            else if (word.Delimeters.Count == 1)
            {
                var d1 = word.Delimeters[0];

                var exp = ParseNode("eq", new List<ExpressionPair>()
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

        private static RqlExpression Reduce(IList<ExpressionPair> expressions)
        {
            var orGroups = new LinkedList<RqlExpression>();
            var currentGroup = new LinkedList<RqlExpression>();
            GroupType? currentType = null;
            foreach (var item in expressions)
            {
                if (!currentType.HasValue || item.Type == currentType)
                {
                    currentGroup.AddLast(item.Expression);
                }
                else
                {
                    // AND -> OR
                    if (item.Type == GroupType.Or)
                    {
                        if (currentGroup.Count > 1)
                        {
                            var andExp = RqlExpression.And(currentGroup);
                            orGroups.AddLast(andExp);
                            currentGroup.Clear();
                        }

                        currentGroup.AddLast(item.Expression);
                    }
                    // OR -> AND
                    else
                    {
                        var prev = currentGroup.Last;
                        if (prev != null)
                            currentGroup.RemoveLast();

                        foreach (var or in currentGroup)
                            orGroups.AddLast(or);

                        currentGroup.Clear();
                        if (prev != null)
                            currentGroup.AddLast(prev);
                        currentGroup.AddLast(item.Expression);
                    }
                }

                currentType = item.Type;
            }


            // transfer remaining items from current group
            if (currentGroup.Count > 1 && currentType == GroupType.And)
            {
                orGroups.AddLast(RqlExpression.And(currentGroup));
            }
            else
            {
                foreach (var or in currentGroup)
                    orGroups.AddLast(or);
            }

            if (orGroups.Count == 1)
                return orGroups.First!.Value;
            return RqlExpression.Or(orGroups);
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

        private static RqlExpression ParseNode(string word, IList<ExpressionPair> inner)
        {
            return word.ToLower(CultureInfo.InvariantCulture) switch
            {
                "" => Reduce(inner),
                "and" => RqlExpression.And(PairToExpressions(inner)),
                "or" => RqlExpression.Or(PairToExpressions(inner)),
                "in" => MakeBinary(RqlExpression.ListIn, inner, static (left, right) =>
                {
                    // in case of in(prop,(val)) list gets reduced to constant
                    // and has to be turned back into group
                    if (right is RqlConstant)
                        right = RqlExpression.Group(string.Empty, right);

                    return (left, right);
                }),
                "out" => MakeBinary(RqlExpression.ListOut, inner),
                "eq" => MakeBinary(RqlExpression.Equal, inner),
                "ne" => MakeBinary(RqlExpression.NotEqual, inner),
                "gt" => MakeBinary(RqlExpression.GreaterThan, inner),
                "ge" => MakeBinary(RqlExpression.GreaterThanOrEqual, inner),
                "lt" => MakeBinary(RqlExpression.LessThan, inner),
                "le" => MakeBinary(RqlExpression.LessThanOrEqual, inner),
                "like" => MakeBinary(RqlExpression.Like, inner),
                "ilike" => MakeBinary(RqlExpression.LikeInsensitive, inner),
                "not" => MakeUnary(RqlExpression.Not, inner),
                "isnull" => MakeUnary(RqlExpression.IsNull, inner),
                var any => RqlExpression.Group(any, PairToExpressions(inner))
            };

            static RqlBinary MakeBinary(Func<RqlExpression, RqlExpression, RqlBinary> func, IList<ExpressionPair> inner,
                Func<RqlExpression, RqlExpression, (RqlExpression, RqlExpression)>? visit = null)
            {
                if (inner.Count != 2)
                    throw new Exception("Binary expression must have 2 arguments");

                var left = inner[0].Expression;
                var right = inner[1].Expression;

                if (visit != null)
                {
                    (left, right) = visit(left, right);
                }

                return func(left, right);
            }

            static RqlUnary MakeUnary(Func<RqlExpression, RqlUnary> func, IList<ExpressionPair> inner,
                Func<RqlExpression, RqlExpression>? visit = null)
            {
                if (inner.Count != 1)
                    throw new Exception("Unary expression must have 1 argument");

                var exp = inner[0].Expression;

                if (visit != null)
                {
                    exp = visit(exp);
                }

                return func(exp);
            }

            static IEnumerable<RqlExpression> PairToExpressions(IList<ExpressionPair> inner)
                => inner.Select(s => s.Expression);
        }
    }
}
