using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Parsers.Linear.Domain.Core;
using Mpt.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace Mpt.Rql.Parsers.Linear.Domain.Services;

internal static class RqlArgumentParser
{
    private static readonly Dictionary<string, Func<RqlArgument>> _expressionFunctionMap = new()
    {
        { Constants.RqlTerm.Empty, RqlExpression.Empty },
        { Constants.RqlTerm.Null, RqlExpression.Null },
    };

    internal static RqlArgument Parse(string term, IList<ExpressionPair> innerExpressionPairs)
    {
        if (innerExpressionPairs.Count > 0)
            throw new RqlArgumentParserException("Constant functions cannot have arguments");

        if (!_expressionFunctionMap.TryGetValue(term, out var resolvedExpression))
            throw new RqlArgumentParserException($"Constant parser does not recognise term '{term}'");

        return resolvedExpression();
    }
}
