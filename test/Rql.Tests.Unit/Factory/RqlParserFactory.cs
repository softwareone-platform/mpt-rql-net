using System.Globalization;
using Moq;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;

namespace Rql.Tests.Unit.Factory;

internal static class RqlParserFactory
{
    internal static IRqlParser RqlEqual(string property, string value)
    {
        var rqlExpression = new RqlEqual(new RqlConstant(property), new RqlConstant(value));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlEqual() => RqlEqual("name", "Jewelry Widget");

    internal static IRqlParser RqlNotEqual()
    {
        var rqlExpression = new RqlNotEqual(new RqlConstant("name"), new RqlConstant("Jewelry Widget"));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlList(bool isIn, string property, params string[] values)
    {
        var member = RqlExpression.Constant(property);
        var arguments = RqlExpression.Group(string.Empty, values.Select(s => RqlExpression.Constant(s)));
        RqlExpression rqlExpression = isIn ? RqlExpression.ListIn(member, arguments) : RqlExpression.ListOut(member, arguments);

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlGreaterThan(decimal value)
    {
        var rqlExpression = new RqlGreaterThan(new RqlConstant("price"),
            new RqlConstant(value.ToString(CultureInfo.InvariantCulture)));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlGreaterEqualThan(decimal value)
    {
        var rqlExpression = new RqlGreaterThanOrEqual(new RqlConstant("price"),
            new RqlConstant(value.ToString(CultureInfo.InvariantCulture)));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlLessThan(decimal value)
    {
        var rqlExpression = new RqlLessThan(new RqlConstant("price"),
            new RqlConstant(value.ToString(CultureInfo.InvariantCulture)));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlLessEqualThan(decimal value)
    {
        var rqlExpression = new RqlLessThanOrEqual(new RqlConstant("price"),
            new RqlConstant(value.ToString(CultureInfo.InvariantCulture)));

        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser BuildRqlParserMock(RqlExpression rqlExpression)
    {
        var parserMock = new Mock<IRqlParser>();
        var parserResult = new List<RqlExpression> { rqlExpression };
        parserMock.Setup(parser => parser.Parse(It.IsAny<string>())).Returns(RqlExpression.Group("", parserResult));

        return parserMock.Object;
    }

    internal static IRqlParser RqlLike(string property, string searchString, bool insesitive = false)
    {
        var member = RqlExpression.Constant(property);
        var value = RqlExpression.Constant(searchString);
        RqlExpression rqlExpression = insesitive ? RqlExpression.LikeCaseInsensitive(member, value)
            : RqlExpression.Like(member, value);
        return BuildRqlParserMock(rqlExpression);
    }

    internal static IRqlParser RqlLike(string searchString) => RqlLike("name", searchString, false);

    internal static IRqlParser RqlILike(string searchString)
    {
        var rqlExpression = new RqlLikeCaseInsensitive(new RqlConstant("name"), new RqlConstant(searchString));

        return BuildRqlParserMock(rqlExpression);
    }
}