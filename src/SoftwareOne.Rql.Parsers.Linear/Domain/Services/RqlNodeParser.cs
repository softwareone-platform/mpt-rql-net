using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;
using System.Globalization;

namespace SoftwareOne.Rql.Parsers.Linear;

internal static class RqlNodeParser
{
    internal static RqlExpression Parse(string word, IList<ExpressionPair> expressionPairList)
    {
        return word.ToLower(CultureInfo.InvariantCulture) switch
        {
            Constants.RqlTerm.Empty => RqlExpressionReducer.Reduce(expressionPairList),
            Constants.RqlTerm.And => RqlExpression.And(ConvertToIEnumerableCollection(expressionPairList)),
            Constants.RqlTerm.Or => RqlExpression.Or(ConvertToIEnumerableCollection(expressionPairList)),
            Constants.RqlTerm.Not => RqlUnaryParser.Parse(Constants.RqlTerm.Not, expressionPairList),
            Constants.RqlTerm.ListIn => RqlBinaryParser.Parse(Constants.RqlTerm.ListIn, expressionPairList),
            Constants.RqlTerm.ListOut => RqlBinaryParser.Parse(Constants.RqlTerm.ListOut, expressionPairList),
            Constants.RqlTerm.Equal => RqlBinaryParser.Parse(Constants.RqlTerm.Equal, expressionPairList),
            Constants.RqlTerm.NotEqual => RqlBinaryParser.Parse(Constants.RqlTerm.NotEqual, expressionPairList),
            Constants.RqlTerm.GreaterThan => RqlBinaryParser.Parse(Constants.RqlTerm.GreaterThan, expressionPairList),
            Constants.RqlTerm.GreaterThanOrEqual => RqlBinaryParser.Parse(Constants.RqlTerm.GreaterThanOrEqual, expressionPairList),
            Constants.RqlTerm.LessThan => RqlBinaryParser.Parse(Constants.RqlTerm.LessThan, expressionPairList),
            Constants.RqlTerm.LessThanOrEqual => RqlBinaryParser.Parse(Constants.RqlTerm.LessThanOrEqual, expressionPairList),
            Constants.RqlTerm.Like => RqlBinaryParser.Parse(Constants.RqlTerm.Like, expressionPairList),
            Constants.RqlTerm.LikeCaseInsensitive => RqlBinaryParser.Parse(Constants.RqlTerm.LikeCaseInsensitive, expressionPairList),
            var any => RqlExpression.Group(any, expressionPairList.Select(s => s.Expression))
        };
    }

    private static IEnumerable<RqlExpression> ConvertToIEnumerableCollection(IList<ExpressionPair> expressionPairs)
        => expressionPairs.Select(expressionPair => expressionPair.Expression);
}