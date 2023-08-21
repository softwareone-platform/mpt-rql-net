using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Parsers.Linear;

namespace Rql.Tests.Unit.Factory;

internal static class RqlExpressionFactory
{
    internal static List<ExpressionPair> Default()
    {
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.And, new RqlConstant("name")),
            new ExpressionPair(GroupType.And, new RqlConstant("Jewelry Widget"))
        };
    }

    internal static List<ExpressionPair> DefaultMultiple()
    {
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.And, new RqlConstant("name")),
            new ExpressionPair(GroupType.And, new RqlAnd(
                new List<RqlExpression> {
                    new RqlConstant("listItem1"),
                    new RqlConstant("listItem2") }))
        };
    }

    internal static List<ExpressionPair> InvalidOnlySingleExpression()
    {
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.And, new RqlConstant("name"))
        };
    }
}