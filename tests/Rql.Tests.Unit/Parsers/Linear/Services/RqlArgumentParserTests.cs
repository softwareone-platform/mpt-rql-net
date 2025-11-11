using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Parsers.Linear.Core;
using Mpt.Rql.Parsers.Linear.Services;
using Rql.Tests.Common.Factory;
using Xunit;

namespace Rql.Tests.Unit.Parsers.Linear.Services;

public class RqlArgumentParserTests
{
    public RqlArgumentParserTests()
    {
    }

    [Theory]
    [InlineData(Constants.RqlTerm.Empty, typeof(RqlEmpty))]
    [InlineData(Constants.RqlTerm.Null, typeof(RqlNull))]
    public void Parse_WhenSuccessfulEmptyInput_ResolvesToRqlEmpty(string term, Type resultType)
    {
        // Act
        var actualResult = RqlArgumentParser.Parse(term, RqlExpressionFactory.EmptyList());

        // Assert
        Assert.IsType(resultType, actualResult);
    }

    [Fact]
    public void Parse_WhenInvalidEmptyInput_ThrowsRqlArgumentParserException()
    {
        // Act and Assert
        Assert.Throws<RqlArgumentParserException>(() => RqlArgumentParser.Parse(Constants.RqlTerm.Self, RqlExpressionFactory.EmptyList()));
    }

    [Fact]
    public void Parse_WhenInvalidEmptyParametersInput_ThrowsRqlArgumentParserException()
    {
        // Act and Assert
        Assert.Throws<RqlArgumentParserException>(() => RqlArgumentParser.Parse(Constants.RqlTerm.Empty, RqlExpressionFactory.FromSingleItem(RqlExpression.Constant("foo"))));
    }
}