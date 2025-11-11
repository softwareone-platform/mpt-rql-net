using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Parsers.Linear.Core;
using Mpt.Rql.Parsers.Linear.Core.ValueTypes;

namespace Mpt.Rql.Parsers.Linear.Services;

internal static class RqlCollectionParser
{
    private static readonly Dictionary<string, Func<RqlExpression, RqlExpression?, RqlCollection>> _expressionFunctionMap = new()
    {
        { Constants.RqlTerm.Any, RqlExpression.Any},
        { Constants.RqlTerm.All, RqlExpression.All!}
    };

    internal static RqlExpression Parse(string term, IList<ExpressionPair> innerExpressionPairs)
    {
        var left = innerExpressionPairs[0].Expression;

        if (!_expressionFunctionMap.TryGetValue(term, out var resolvedExpression))
            throw new RqlCollectionParserException($"Collection parser does not recognise term '{term}'");

        RqlExpression? right = innerExpressionPairs.Count > 1 ? innerExpressionPairs[1].Expression : null;

        return resolvedExpression(left, right);
    }
}
