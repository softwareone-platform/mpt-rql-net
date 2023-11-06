using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Exception;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Services;

public class RqlParser : IRqlParser
{
    private static readonly HashSet<char> _textDelimiters = new() { '"', '\'' };
    private static readonly Dictionary<char, GroupType> _operatorToType = new()
    {
        { ',' , GroupType.And },
        { '&' , GroupType.And },
        { ';' , GroupType.Or },
        { '|' , GroupType.Or }
    };

    public RqlGroup Parse(string expression)
    {
        var exp = RqlExpressionReducer.Reduce(ParseInternal(expression.AsMemory(), 0, out int _, false));
        if (exp is RqlGroup grp)
            return grp;
        return RqlExpression.Group("", exp);
    }

    private static IList<ExpressionPair> ParseInternal(ReadOnlyMemory<char> query, int startIndex, out int currentIndex, bool takeOne)
    {
        currentIndex = startIndex;
        var expressions = new List<ExpressionPair>();

        char currentSymbol;
        var word = Word.Make(query, startIndex);
        var querySpan = query.Span;

        while (currentIndex < query.Length)
        {
            currentSymbol = querySpan[currentIndex];
            if (currentSymbol == '=' && word.WrapSymbol == null)
            {
                HandleEqualsShortcut(query, ref currentIndex, ref word, expressions);
                continue;
            }
            else if (word.WrapSymbol != null)
            {
                HandleWrapEnd(currentSymbol, currentIndex, ref word);
            }
            else if (_textDelimiters.Contains(currentSymbol))
            {
                HandleWrapStart(currentSymbol, currentIndex, ref word);
            }
            else if (_operatorToType.TryGetValue(currentSymbol, out var nextType))
            {
                HandleGroupOperator(query, currentIndex, ref word, nextType, expressions);
            }
            else if (currentSymbol == '(')
            {
                HandleParenthesesStart(query, ref currentIndex, ref word, expressions);
            }
            else if (currentSymbol == ')')
            {
                HandleParenthesesEnd(query, ref currentIndex, ref word, expressions);
                break;
            }
            else
            {
                word.WordLength++;
            }

            if (takeOne && expressions.Count > 0)
                return expressions;

            currentIndex++;
        }

        HandleParenthesesEnd(query, ref currentIndex, ref word, expressions);

        return expressions;
    }

    private static void HandleEqualsShortcut(ReadOnlyMemory<char> query, ref int currentIndex, ref Word word, List<ExpressionPair> expressions)
    {
        RqlExpression? left;
        
        var replaceLastExpression = word.WordLength == 0 && expressions.Count > 0;

        if (replaceLastExpression)
        {
            var lastPair = expressions[^1];
            left = lastPair.Expression;
            expressions.Remove(lastPair);
        }
        else
        {
            left = RqlExpressionMapper.MapFromWord(word);
        }

        var rightNodes = ParseInternal(query, currentIndex + 1, out currentIndex, true);
        if (rightNodes.Count != 1 || rightNodes[0].Expression is not RqlArgument)
            throw new RqlParserException("Invalid equals shortcut expression");
        expressions.Add(new ExpressionPair(word.GroupType, RqlExpression.Equal(left, rightNodes[0].Expression)));
        word = Word.Make(query, currentIndex + 1);
    }

    private static void HandleGroupOperator(ReadOnlyMemory<char> query, int currentIndex, ref Word word, GroupType groupType, List<ExpressionPair> expressions)
    {
        if (word.WordLength > 0)
        {
            var exp = RqlExpressionMapper.MapFromWord(word);
            expressions.Add(new ExpressionPair(word.GroupType, exp));
        }

        word = Word.Make(query, currentIndex + 1);
        word.GroupType = groupType;
    }

    private static void HandleWrapStart(char currentSymbol, int currentIndex, ref Word word)
    {
        word.WrapSymbol = currentSymbol;
        word.WrapStart = currentIndex;
        word.WordLength++;
    }

    private static void HandleWrapEnd(char currentSymbol, int currentIndex, ref Word word)
    {
        if (currentSymbol == word.WrapSymbol)
        {
            word.WrapEnd = currentIndex;
            word.WrapSymbol = null;
        }
        word.WordLength++;
    }

    private static void HandleParenthesesStart(ReadOnlyMemory<char> query, ref int currentIndex, ref Word word, List<ExpressionPair> expressions)
    {
        var innerNodes = ParseInternal(query, currentIndex + 1, out currentIndex, false);
        var node = RqlNodeParser.Parse(word.ToString(), innerNodes);
        expressions.Add(new ExpressionPair(word.GroupType, node));

        word = Word.Make(query, currentIndex + 1);
    }

    private static void HandleParenthesesEnd(ReadOnlyMemory<char> query, ref int currentIndex, ref Word word, List<ExpressionPair> expressions)
    {
        if (word.WordLength > 0)
            expressions.Add(new ExpressionPair(word.GroupType, RqlExpressionMapper.MapFromWord(word)));

        word = Word.Make(query, currentIndex + 1);
    }
}