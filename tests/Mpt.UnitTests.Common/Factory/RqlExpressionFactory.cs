using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Parsers.Linear.Domain.Core.Enumerations;
using Mpt.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace Mpt.UnitTests.Common;

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

    internal static List<ExpressionPair> SingleDefault()
    {
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.And, new RqlAnd(
                new List<RqlExpression> {
                    new RqlConstant("listItem1"),
                    new RqlConstant("listItem2") }))
        };
    }

    internal static List<ExpressionPair> SingleWithGroup()
    {
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.None, new RqlGenericGroup(string.Empty,
                new List<RqlExpression> {
                    new RqlConstant("listItem1"),
                    new RqlConstant("listItem2") }))
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

    internal static List<ExpressionPair> AndOrAnd()
    {
        // Represents 'field1=value1&field2=value2|field3=value3&field4=value4'
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.None, new RqlEqual(new RqlConstant("field1"), new RqlConstant("value1"))),
            new ExpressionPair(GroupType.And, new RqlEqual(new RqlConstant("field2"), new RqlConstant("value2"))),
            new ExpressionPair(GroupType.Or, new RqlEqual(new RqlConstant("field3"), new RqlConstant("value3"))),
            new ExpressionPair(GroupType.And, new RqlEqual(new RqlConstant("field4"), new RqlConstant("value4")))
        };
    }

    internal static List<ExpressionPair> OrAndOr()
    {
        // Represents 'field1=value1|field2=value2&field3=value3|field4=value4'
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.None, new RqlEqual(new RqlConstant("field1"), new RqlConstant("value1"))),
            new ExpressionPair(GroupType.Or, new RqlEqual(new RqlConstant("field2"), new RqlConstant("value2"))),
            new ExpressionPair(GroupType.And, new RqlEqual(new RqlConstant("field3"), new RqlConstant("value3"))),
            new ExpressionPair(GroupType.Or, new RqlEqual(new RqlConstant("field4"), new RqlConstant("value4")))
        };
    }

    internal static List<ExpressionPair> OrBracketsAndOrBrackets()
    {
        // Represents '(field1=value1|field2=value2)&(field3=value3|field4=value4)'
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.None,
                new RqlOr(
                    new List<RqlExpression> {
                        new RqlEqual(new RqlConstant("field1"), new RqlConstant("value1")),
                        new RqlEqual(new RqlConstant("field2"), new RqlConstant("value2")) })),
            new ExpressionPair(GroupType.And,
                new RqlOr(
                    new List<RqlExpression> {
                        new RqlEqual(new RqlConstant("field3"), new RqlConstant("value3")),
                        new RqlEqual(new RqlConstant("field4"), new RqlConstant("value4")) }))
        };
    }

    internal static List<ExpressionPair> EmptyList()
    {
        return new List<ExpressionPair>();
    }

    internal static List<ExpressionPair> FromSingleItem(RqlExpression expression)
    {
        return new List<ExpressionPair>() { new ExpressionPair { Expression = expression, Type = GroupType.None } };
    }

    internal static List<ExpressionPair> ConstantList(int count)
    {
        return Enumerable.Range(0, count).Select(s => new ExpressionPair(GroupType.None, RqlExpression.Constant($"constant_{s}"))).ToList();
    }

    internal static List<ExpressionPair> InvalidOnlySingleExpression()
    {
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.And, new RqlConstant("name"))
        };
    }
}