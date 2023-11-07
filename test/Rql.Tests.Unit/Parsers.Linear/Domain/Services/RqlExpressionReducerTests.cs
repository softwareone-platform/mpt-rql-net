using Rql.Tests.Unit.Factory;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Parsers.Linear.Domain.Services;
using Xunit;

namespace Rql.Tests.Unit.Parsers.Linear.Domain.Services;

public class RqlExpressionReducerTests
{
    [Fact]
    public void Reduce_WithAndOrAnd_ReturnsOr()
    {
        // Act
        var actualResult = RqlExpressionReducer.Reduce(RqlExpressionFactory.AndOrAnd());

        // Assert
        var or = Assert.IsType<RqlOr>(actualResult);
        var andLeft = Assert.IsType<RqlAnd>(or.Items?[0]);
        var andRight = Assert.IsType<RqlAnd>(or.Items?[1]);
        Assert.IsType<RqlEqual>(andLeft.Items?[0]);
        Assert.IsType<RqlEqual>(andLeft.Items?[1]);
        Assert.IsType<RqlEqual>(andRight.Items?[0]);
        Assert.IsType<RqlEqual>(andRight.Items?[1]);
    }

    [Fact]
    public void Reduce_WithOrAndOr_ReturnsEqualWithAndWithEqualRqlExpressions()
    {
        // Act
        var actualResult = RqlExpressionReducer.Reduce(RqlExpressionFactory.OrAndOr());

        // Assert
        var or = Assert.IsType<RqlOr>(actualResult);
        Assert.IsType<RqlEqual>(or.Items?[0]);
        Assert.IsType<RqlAnd>(or.Items?[1]);
        Assert.IsType<RqlEqual>(or.Items?[2]);
    }

    [Fact]
    public void Reduce_WithOrBracketsAndOrBrackets_ReturnsAndWithOrSubRqlExpressions()
    {
        // Act
        var actualResult = RqlExpressionReducer.Reduce(RqlExpressionFactory.OrBracketsAndOrBrackets());

        // Assert
        var and = Assert.IsType<RqlAnd>(actualResult);
        Assert.IsType<RqlOr>(and.Items?[0]);
        Assert.IsType<RqlOr>(and.Items?[1]);
    }
}