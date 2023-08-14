using Moq;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;

namespace Rql.Tests.Unit.Factory;

internal static class RqlParserFactory
{
    internal static IRqlParser RqlEqual()
    {
        var rqlExpression = new RqlEqual(new RqlConstant("name"), new RqlConstant("Jewelry Widget"));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlNotEqual()
    {
        var rqlExpression = new RqlNotEqual(new RqlConstant("name"), new RqlConstant("Jewelry Widget"));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlGreaterThan(decimal value)
    {
        var rqlExpression = new RqlGreaterThan(new RqlConstant("price"), new RqlConstant(value.ToString()));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlGreaterEqualThan(decimal value)
    {
        var rqlExpression = new RqlGreaterThanOrEqual(new RqlConstant("price"), new RqlConstant(value.ToString()));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlLessThan(decimal value)
    {
        var rqlExpression = new RqlLessThan(new RqlConstant("price"), new RqlConstant(value.ToString()));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlLessEqualThan(decimal value)
    {
        var rqlExpression = new RqlLessThanOrEqual(new RqlConstant("price"), new RqlConstant(value.ToString()));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser BuildRqlParserMock(RqlExpression rqlExpression)
    {
        var parserMock = new Mock<IRqlParser>();
        var parserResult = new List<RqlExpression> { rqlExpression };
        parserMock.Setup(parser => parser.Parse(It.IsAny<string>())).Returns(RqlExpression.Group("", parserResult));

        return parserMock.Object;
    }

    internal static IRqlParser RqlLike(string searchString)
    {
        var rqlExpression = new RqlLike(new RqlConstant("name"), new RqlConstant(searchString));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlILike(string searchString)
    {
        var rqlExpression = new RqlLikeInsensitive(new RqlConstant("name"), new RqlConstant(searchString));

        return BuildRqlParserMock(rqlExpression);
    }
}