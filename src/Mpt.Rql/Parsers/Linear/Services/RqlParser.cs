using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Parsers.Linear.Core.Enumerations;
using Mpt.Rql.Parsers.Linear.Core.ValueTypes;

namespace Mpt.Rql.Parsers.Linear.Services;

public class RqlParser : IRqlParser
{
    private static readonly HashSet<char> _textDelimiters = ['"', '\''];
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

    private static List<ExpressionPair> ParseInternal(ReadOnlyMemory<char> query, int startIndex, out int currentIndex, bool takeOne)
    {
        currentIndex = startIndex;
        var expressions = new List<ExpressionPair>();

        char currentSymbol;
        var word = Word.Make(query, startIndex);
        var querySpan = query.Span;

        while (currentIndex < query.Length)
        {
            currentSymbol = querySpan[currentIndex];
            if (currentSymbol == '=' && word.QuoteSymbol == null)
            {
                HandleEqualsShortcut(query, ref currentIndex, ref word, expressions);
                continue;
            }
            else if (word.QuoteSymbol != null)
            {
                HandleQuoteEnd(currentSymbol, currentIndex, ref word);
            }
            else if (_textDelimiters.Contains(currentSymbol))
            {
                HandleQuoteStart(currentSymbol, currentIndex, ref word);
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

    /// <summary>
    /// Handles the equals shortcut syntax, allowing expressions like 'field=value' instead of 'eq(field,value)'.
    /// </summary>
    /// <param name="query">The original query string.</param>
    /// <param name="currentIndex">The current processing index.</param>
    /// <param name="word">The collected word.</param>
    /// <param name="expressions">The collection of collected expressions.</param>
    /// <exception cref="RqlParserException">Thrown when the right side expression cannot be resolved into a single <see cref="RqlArgument"/>.</exception>
    private static void HandleEqualsShortcut(ReadOnlyMemory<char> query, ref int currentIndex, ref Word word, List<ExpressionPair> expressions)
    {
        RqlExpression? left;

        var replaceLastExpression = word.WordLength == 0 && expressions.Count > 0;

        // If the left side is represented by an already resolved expression '(property)', rather than a word, take that expression.
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

        // A plain word on the right side leaves currentIndex on the delimiter that ended it; a parenthesised right
        // side (function, pointer, or a call with a member path such as first(orders).id) leaves it on its last
        // consumed character, so step past that. The loop resumes at currentIndex without incrementing, so the next
        // word must start exactly there; structural symbols re-make it anyway.
        if (rightNodes[0].Expression is RqlFunction or RqlPointer)
        {
            currentIndex++;
            word = Word.Make(query, currentIndex);
        }
        else
        {
            word = Word.Make(query, currentIndex + 1);
        }
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

    private static void HandleQuoteStart(char currentSymbol, int currentIndex, ref Word word)
    {
        word.QuoteSymbol = currentSymbol;
        word.QuoteStart = currentIndex;
        word.IsQuoted = true;
        word.WordLength++;
    }

    private static void HandleQuoteEnd(char currentSymbol, int currentIndex, ref Word word)
    {
        if (currentSymbol == word.QuoteSymbol)
        {
            word.QuoteEnd = currentIndex;
            word.QuoteSymbol = null;
        }
        word.WordLength++;
    }

    private static void HandleParenthesesStart(ReadOnlyMemory<char> query, ref int currentIndex, ref Word word, List<ExpressionPair> expressions)
    {
        var innerNodes = ParseInternal(query, currentIndex + 1, out currentIndex, false);
        var node = RqlNodeParser.Parse(word.ToString(), innerNodes);

        // A dotted path directly after the closing parenthesis is a member access on the call's result:
        // first(parameters,eq(name,"priority")).value
        var memberPath = ReadMemberPath(query, currentIndex + 1, out var memberPathEnd);
        if (memberPathEnd > currentIndex)
        {
            if (memberPath.Length > 0)
                node = RqlExpression.Member(node, memberPath);
            currentIndex = memberPathEnd; // a bare trailing dot is consumed and ignored
        }

        expressions.Add(new ExpressionPair(word.GroupType, node));

        word = Word.Make(query, currentIndex + 1);
    }

    /// <summary>
    /// Reads <c>.a.b.c</c> starting at <paramref name="start"/> (which must hold the dot) up to the next structural
    /// character; returns the path without the leading dot and the index of its last consumed character (the dot
    /// itself when nothing follows it). Returns an empty path, with <paramref name="lastIndex"/> before <paramref name="start"/>,
    /// when there is no dot.
    /// </summary>
    private static string ReadMemberPath(ReadOnlyMemory<char> query, int start, out int lastIndex)
    {
        lastIndex = start - 1;
        var span = query.Span;
        if (start >= span.Length || span[start] != '.')
            return string.Empty;

        var end = start + 1;
        while (end < span.Length && span[end] != '(' && span[end] != ')' && span[end] != '=' && !_operatorToType.ContainsKey(span[end]) && !_textDelimiters.Contains(span[end]))
            end++;

        lastIndex = end - 1;
        if (end == start + 1)
            return string.Empty;
        return query[(start + 1)..end].ToString();
    }

    private static void HandleParenthesesEnd(ReadOnlyMemory<char> query, ref int currentIndex, ref Word word, List<ExpressionPair> expressions)
    {
        if (word.WordLength > 0)
            expressions.Add(new ExpressionPair(word.GroupType, RqlExpressionMapper.MapFromWord(word)));

        word = Word.Make(query, currentIndex + 1);
    }
}