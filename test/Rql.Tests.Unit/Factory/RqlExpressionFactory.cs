using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

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

    internal static List<ExpressionPair> InvalidOnlySingleExpression()
    {
        return new List<ExpressionPair>()
        {
            new ExpressionPair(GroupType.And, new RqlConstant("name"))
        };
    }
}