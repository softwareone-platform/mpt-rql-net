using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Parsers.Linear.Domain.Core;
using Mpt.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace Mpt.Rql.Parsers.Linear.Domain.Services;

internal static class RqlPointerParser
{
    private static readonly Dictionary<string, Func<RqlExpression?, RqlPointer>> _expressionFunctionMap = new()
    {
        { Constants.RqlTerm.Self, RqlExpression.Self },
    };

    internal static RqlPointer Parse(string term, IList<ExpressionPair> innerExpressionPairs)
    {
        if (innerExpressionPairs.Count > 1)
            throw new RqlPointerParserException("Pointer expression must have 0 or 1 argument");

        ExpressionPair? inner = innerExpressionPairs.Count > 0 ? innerExpressionPairs[0] : null;

        if (!_expressionFunctionMap.TryGetValue(term, out var resolvedExpression))
            throw new RqlPointerParserException($"Pointer parser does not recognise term '{term}'");

        return resolvedExpression(inner?.Expression);
    }
}
