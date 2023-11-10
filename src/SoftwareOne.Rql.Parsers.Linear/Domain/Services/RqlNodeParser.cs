using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;
using System.Globalization;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Services;

internal static class RqlNodeParser
{
    internal static RqlExpression Parse(string word, IList<ExpressionPair> expressionPairList)
    {
        var loweredWord = word.ToLower(CultureInfo.InvariantCulture);
        return loweredWord switch
        {
            Constants.RqlTerm.NoName => RqlExpressionReducer.Reduce(expressionPairList),
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
            Constants.RqlTerm.Any => RqlCollectionParser.Parse(Constants.RqlTerm.Any, expressionPairList),
            Constants.RqlTerm.All => RqlCollectionParser.Parse(Constants.RqlTerm.All, expressionPairList),
            Constants.RqlTerm.Self => RqlPointerParser.Parse(Constants.RqlTerm.Self, expressionPairList),
            Constants.RqlTerm.Empty => RqlArgumentParser.Parse(Constants.RqlTerm.Empty, expressionPairList),
            Constants.RqlTerm.Null => RqlArgumentParser.Parse(Constants.RqlTerm.Null, expressionPairList),
            _ => RqlExpression.Group(loweredWord, expressionPairList.Select(s => s.Expression))
        };
    }

    private static IEnumerable<RqlExpression> ConvertToIEnumerableCollection(IList<ExpressionPair> expressionPairs)
        => expressionPairs.Select(expressionPair => expressionPair.Expression);
}