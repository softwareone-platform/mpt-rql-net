using Xunit;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Parsers.Linear;
using Rql.Tests.Unit.Factory;
using SoftwareOne.Rql.Parsers.Linear.Domain;
using SoftwareOne.Rql.Abstractions.Exception;

namespace Rql.Tests.Unit.Parsers.Linear.Domain.Services;

public class RqlBinaryParserTests
{
    public RqlBinaryParserTests()
    {
    }

    [Fact]
    public void Parse_WhenSuccessfulSingleListInInput_ResolvesToRqlListIn()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.ListIn, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlListIn), actualResult.GetType());
        Assert.Equal(typeof(RqlGenericGroup), ((RqlListIn)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulMultipleListInInput_ResolvesToRqlListIn()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.ListIn, RqlExpressionFactory.DefaultMultiple());

        // Assert
        Assert.Equal(typeof(RqlListIn), actualResult.GetType());
        Assert.Equal(typeof(RqlAnd), ((RqlListIn)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulEqualInput_ResolvesToRqlEqual()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.Equal, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlEqual), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlEqual)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulSingleListOutInput_ResolvesToRqlListOut()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.ListOut, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlListOut), actualResult.GetType());
        Assert.Equal(typeof(RqlGenericGroup), ((RqlListOut)actualResult).Right.GetType());
    }


    [Fact]
    public void Parse_WhenSuccessfulMultipleListOutInput_ResolvesToRqlListOut()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.ListOut, RqlExpressionFactory.DefaultMultiple());

        // Assert
        Assert.Equal(typeof(RqlListOut), actualResult.GetType());
        Assert.Equal(typeof(RqlAnd), ((RqlListOut)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulNotEqualInput_ResolvesToRqlNotEqual()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.NotEqual, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlNotEqual), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlNotEqual)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulGreaterThanlInput_ResolvesToRqlGreaterThan()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.GreaterThan, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlGreaterThan), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlGreaterThan)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulGreaterThanOrEquallInput_ResolvesToRqlGreaterThanOrEqual()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.GreaterThanOrEqual, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlGreaterThanOrEqual), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlGreaterThanOrEqual)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulLessThanlInput_ResolvesToRqlLessThan()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.LessThan, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlLessThan), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlLessThan)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulLikeInput_Resolves_To_RqlLike()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.Like, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlLike), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlLike)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulLikeCaseInsensitiveInput_ResolvesToRqlLikeCaseInsensitive()
    {
        // Act
        var actualResult = RqlBinaryParser.Parse(Constants.RqlTerm.LikeCaseInsensitive, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlLikeCaseInsensitive), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlLikeCaseInsensitive)actualResult).Right.GetType());
    }

    [Fact]
    public void Parse_WhenInvalidExpression_ThrowsRqlBinaryParserException()
    {
        // Act
        Assert.Throws<RqlBinaryParserException>(() =>
            RqlBinaryParser.Parse(Constants.RqlTerm.LikeCaseInsensitive, RqlExpressionFactory.InvalidOnlySingleExpression()));
    }

    [Fact]
    public void Parse_WhenInvalidRqlTerm_ThrowsRqlBinaryParserException()
    {
        // Act
        Assert.Throws<RqlBinaryParserException>(() =>
            RqlBinaryParser.Parse("InvalidTerm", RqlExpressionFactory.Default()));
    }
}