using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Parsers.Linear.Core;
using Mpt.Rql.Parsers.Linear.Core.ValueTypes;

namespace Mpt.Rql.Parsers.Linear.Services;

internal static class RqlBinaryParser
{
    private static readonly Dictionary<string, Func<RqlExpression, RqlExpression, RqlBinary>> _expressionFunctionMap = new()
    {
        { Constants.RqlTerm.ListIn, RqlExpression.ListIn },
        { Constants.RqlTerm.ListOut, RqlExpression.ListOut },
        { Constants.RqlTerm.Equal, RqlExpression.Equal},
        { Constants.RqlTerm.NotEqual, RqlExpression.NotEqual},
        { Constants.RqlTerm.GreaterThan, RqlExpression.GreaterThan},
        { Constants.RqlTerm.GreaterThanOrEqual, RqlExpression.GreaterThanOrEqual},
        { Constants.RqlTerm.LessThan, RqlExpression.LessThan},
        { Constants.RqlTerm.LessThanOrEqual, RqlExpression.LessThanOrEqual},
        { Constants.RqlTerm.Like, RqlExpression.Like},
        { Constants.RqlTerm.LikeCaseInsensitive, RqlExpression.LikeCaseInsensitive},
    };

    internal static RqlExpression Parse(string term, IList<ExpressionPair> innerExpressionPairs)
    {
        if (innerExpressionPairs.Count != 2)
            throw new RqlBinaryParserException("Binary expression must have 2 arguments");

        var left = innerExpressionPairs[0].Expression;

        if (!_expressionFunctionMap.TryGetValue(term, out var resolvedExpression))
            throw new RqlBinaryParserException($"Binary parser does not recognise term '{term}'");

        // There is an exception for resolving the right expression of the RqlBinary for ListIn
        // i.e. in the case of in(prop, (singleListValue)) where singleListValue is just a single value list, this has to be turned back into group
        var right = (resolvedExpression == RqlExpression.ListIn || resolvedExpression == RqlExpression.ListOut) &&
                innerExpressionPairs[1].Expression is RqlConstant
            ? RqlExpression.Group(string.Empty, innerExpressionPairs[1].Expression)
            : innerExpressionPairs[1].Expression;

        return resolvedExpression(left, right);
    }
}
