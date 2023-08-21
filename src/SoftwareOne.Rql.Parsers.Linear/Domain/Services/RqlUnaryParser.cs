using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Unary;

namespace SoftwareOne.Rql.Parsers.Linear;

internal static class RqlUnaryParser
{
    private static readonly Dictionary<string, Func<RqlExpression, RqlUnary>> _expressionFunctionMap;

    static RqlUnaryParser()
    {
        _expressionFunctionMap = new Dictionary<string, Func<RqlExpression, RqlUnary>>
            {
                { "not", RqlExpression.Not }
            };
    }

    internal static RqlExpression Parse(string word, IList<ExpressionPair> innerExpressions)
    {
        if (innerExpressions.Count != 1)
            throw new Exception("Unary expression must have 1 argument");

        var unaryExpression = innerExpressions[0].Expression;
        var resolvedExpression = _expressionFunctionMap[word];

        return resolvedExpression(unaryExpression);
    }
}