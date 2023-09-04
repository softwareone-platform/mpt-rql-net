using SoftwareOne.Rql.Abstractions;
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
        var exp = RqlExpressionReducer.Reduce(ParseInternal(expression.AsMemory(), 0, out int _));
        if (exp is RqlGroup grp)
            return grp;
        return RqlExpression.Group("", exp);
    }

    private static IList<ExpressionPair> ParseInternal(ReadOnlyMemory<char> query, int startIndex, out int currentIndex)
    {
        currentIndex = startIndex;
        var expressions = new List<ExpressionPair>();

        char currentSymbol;
        var word = Word.Make(query, startIndex);
        var querySpan = query.Span;

        while (currentIndex < query.Length)
        {
            currentSymbol = querySpan[currentIndex];
            if (currentSymbol == '=')
                word.Delimiters.Add(currentIndex);

            if (word.WrapSymbol != null)
            {
                HandleDelimiterEnd(currentSymbol, currentIndex, ref word);
            }
            else if (_textDelimiters.Contains(currentSymbol))
            {
                HandleDelimiterStart(currentSymbol, currentIndex, ref word);
            }
            else if (_operatorToType.TryGetValue(currentSymbol, out var nextType))
            {
                HandleGroupOperator(query, currentIndex, ref word, nextType, expressions);
            }
            else if (currentSymbol == '(')
            {
                HandleParenthesesStart(query, ref currentIndex, ref word, expressions);
                continue;
            }
            else if (currentSymbol == ')')
            {
                currentIndex++;
                break;
            }
            else
            {
                word.WordLength++;
            }

            currentIndex++;
        }

        if (word.WordLength > 0)
            expressions.Add(new ExpressionPair(word.GroupType, RqlExpressionMapper.MapFromWord(word)));

        return expressions;
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

    private static void HandleDelimiterStart(char currentSymbol, int currentIndex, ref Word word)
    {
        word.WrapSymbol = currentSymbol;
        word.WrapStart = currentIndex;
        word.WordLength++;
    }

    private static void HandleDelimiterEnd(char currentSymbol, int currentIndex, ref Word word)
    {
        if (currentSymbol == word.WrapSymbol)
            word.WrapEnd = currentIndex;

        word.WordLength++;
    }

    private static void HandleParenthesesStart(ReadOnlyMemory<char> query, ref int currentIndex, ref Word word, List<ExpressionPair> expressions)
    {
        if (query.Span[currentIndex + 1] == ')')
        {
            currentIndex += 2;
            word.WordLength += 2;
        }
        else
        {
            var innerNodes = ParseInternal(query, currentIndex + 1, out currentIndex);
            var node = RqlNodeParser.Parse(word.ToString(), innerNodes);
            expressions.Add(new ExpressionPair(word.GroupType, node));

            word = Word.Make(query, currentIndex + 1);
        }
    }
}