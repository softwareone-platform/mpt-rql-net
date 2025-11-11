using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Abstractions.Unary;
using Mpt.Rql.Parsers.Linear.Core.ValueTypes;

namespace Mpt.Rql.Parsers.Linear.Services;

internal static class RqlUnaryParser
{
    private static readonly Dictionary<string, Func<RqlExpression, RqlUnary>> _expressionFunctionMap = new() { { "not", RqlExpression.Not } };

    internal static RqlExpression Parse(string term, IList<ExpressionPair> innerExpressions)
    {
        if (innerExpressions.Count != 1)
            throw new RqlUnaryParserException("Unary expression must have 1 argument");

        var unaryExpression = innerExpressions[0].Expression;

        if (!_expressionFunctionMap.TryGetValue(term, out var resolvedExpression))
            throw new RqlUnaryParserException($"Unary parser does not recognise term '{term}'");

        return resolvedExpression(unaryExpression);
    }
}