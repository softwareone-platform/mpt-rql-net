using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Unary;
using Mpt.Rql.Parsers.Linear.Domain.Core;
using Mpt.Rql.Parsers.Linear.Domain.Services;
using Mpt.UnitTests.Common.Factory;
using Xunit;

namespace Mpt.Rql.Parsers.Linear.UnitTests.Domain.Services;

public class RqlUnaryParserTests
{
    [Fact]
    public void Parse_WhenSuccessfulSingleNotInput_ReturnsValidResult()
    {
        // Act
        var actualResult = RqlUnaryParser.Parse(Constants.RqlTerm.Not, RqlExpressionFactory.SingleDefault());

        // Assert
        Assert.Equal(typeof(RqlNot), actualResult.GetType());
        Assert.Equal(typeof(RqlAnd), ((RqlNot)actualResult).Nested.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulSingleWithGroupNotInput_ReturnsValidResult()
    {
        // Act
        var actualResult = RqlUnaryParser.Parse(Constants.RqlTerm.Not, RqlExpressionFactory.SingleWithGroup());

        // Assert
        Assert.Equal(typeof(RqlNot), actualResult.GetType());
        Assert.Equal(typeof(RqlGenericGroup), ((RqlNot)actualResult).Nested.GetType());
    }

    [Fact]
    public void Parse_WhenMultipleNotInput_ThrowsException()
    {
        // Act and Assert
        Assert.Throws<RqlUnaryParserException>(() => RqlUnaryParser.Parse(Constants.RqlTerm.Not, RqlExpressionFactory.Default()));
    }

    [Fact]
    public void Parse_WhenInvalidRqlTerm_ThrowsRqlBinaryParserException()
    {
        // Act and Assert
        Assert.Throws<RqlUnaryParserException>(() =>
            RqlUnaryParser.Parse("InvalidTerm", RqlExpressionFactory.Default()));
    }
}