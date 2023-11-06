using Rql.Tests.Unit.Factory;
using Rql.Tests.Unit.Utility;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Services.Filtering;
using Xunit;

namespace Rql.Tests.Unit.Linq.Filtering;

public class FilteringServiceTests
{
    [Theory]
    [InlineData("eq(name,Jewelry Widget)")]
    [InlineData("name=Jewelry Widget")]
    [InlineData("eq(sub.name,Jewelry Widget)")]
    [InlineData("sub.name=Jewelry Widget")]
    public void Apply_WithRqlEqual_ReturnsSingleFilteredResult(string query)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlEqual();
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.Equal();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Single(actualResult.Value);
        Assert.True(actualResult.Value.Single().Name == "Jewelry Widget");
    }

    [Fact]
    public void Apply_WithRqlNotEqual_ReturnsAllButSingleResult()
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlNotEqual();
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.NotEqual();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), "ne(name,Jewelry Widget)");

        // Assert
        Assert.False(actualResult.IsError);
        Assert.True(actualResult.Value.Count() > 1);
        Assert.False(actualResult.Value.Any(_ => _.Name == "Jewelry Widget"));
    }

    [Theory]
    [InlineData("gt(price,129.99)", 129.99)]
    [InlineData("gt(price,200)", 200)]
    public void Apply_WithRqlGreaterThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlGreaterThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.GreaterThan();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.True(actualResult.Value.Count() > 1);
        Assert.False(actualResult.Value.Any(_ => _.Price <= value));
    }

    [Theory]
    [InlineData("gt(price,100000)", 100000)]
    [InlineData("gt(price,12345.67)", 12345.67)]
    public void Apply_WithRqlGreaterThanTooHigh_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlGreaterThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.GreaterThan();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Empty(actualResult.Value);
    }

    [Theory]
    [InlineData("ge(price,129.99)", 129.99)]
    [InlineData("ge(price,200)", 200)]
    public void Apply_WithRqlGreaterEqualThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlGreaterEqualThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.GreaterEqualThan();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.True(actualResult.Value.Count() > 1);
        Assert.False(actualResult.Value.Any(_ => _.Price < value));
    }

    [Theory]
    [InlineData("ge(price,100000)", 100000)]
    [InlineData("ge(price,12345.67)", 12345.67)]
    public void Apply_WithRqlGreaterEqualThanTooHigh_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlGreaterThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.GreaterThan();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Empty(actualResult.Value);
    }

    [Theory]
    [InlineData("lt(price,150.1)", 150.1)]
    [InlineData("lt(price,500)", 500)]
    public void Apply_WithRqlLessThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlLessThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.LessThan();
        var typeMetadataProvider = MetadataProviderFactory.Internal();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.True(actualResult.Value.Count() > 1);
        Assert.False(actualResult.Value.Any(_ => _.Price >= value));
    }

    [Theory]
    [InlineData("lt(price,1)", 1)]
    [InlineData("lt(price,-10000)", -10000)]
    public void Apply_WithRqlLessThanThanTooLow_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlLessThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.LessThan();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Empty(actualResult.Value);
    }

    [Theory]
    [InlineData("le(price,150.1)", 150.1)]
    [InlineData("le(price,500)", 500)]
    public void Apply_WithRqlLessEqualThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlLessEqualThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.LessEqualThan();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.True(actualResult.Value.Count() > 1);
        Assert.False(actualResult.Value.Any(_ => _.Price > value));
    }

    [Theory]
    [InlineData("le(price,1)", 1)]
    [InlineData("le(price,-10000)", -10000)]
    public void Apply_WithRqlLessEqualThanTooLow_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlLessEqualThan(value);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.LessEqualThan();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Empty(actualResult.Value);
    }

    [Theory]
    [InlineData("like(name,*Widget)", "*Widget", 1)]
    [InlineData("like(name,*DOES_NOT_EXIST)", "*DOES_NOT_EXIST", 0)]
    public void Apply_WithRqlLikeMatch_ReturnsExpectedResult(string query, string searchString, int expectedCount)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlLike(searchString);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.Like();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Equal(actualResult.Value.Count(), expectedCount);
    }

    [Theory]
    [InlineData("ilike(name,*WIDGET)", "*WIDGET", 0)]
    [InlineData("ilike(name,*DOES_NOT_EXIST)", "*DOES_NOT_EXIST", 0)]
    public void Apply_WithRqlILikeMatch_ReturnsExpectedResult(string query, string searchString, int expectedCount)
    {
        // Note that 'ilike(name,*WIDGET)' test returns nothing as we are using in memory database implementation and the default .net behaviour
        // is case sensitive. Kept this test in at a unit level for clarity however

        // Arrange
        var parserMock = RqlParserFactory.RqlILike(searchString);
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.ILike();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Equal(actualResult.Value.Count(), expectedCount);
    }


    [Fact]
    public void Apply_WithRqlEqual_OperatorProhibited()
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlEqual("id", "13");
        var operatorHandlerProviderMock = OperatorHandlerProviderFactory.Equal();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityViewOperatorTest>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Operators().AsQueryable(), $"id=13");

        // Assert
        Assert.True(actualResult.IsError);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(RqlOperators.Eq), actualResult.FirstError.Description);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WithRqlList_OperatorProhibited(bool isIn)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlList(isIn, "id", "13", "14", "15");
        var operatorHandlerProviderMock = isIn ? OperatorHandlerProviderFactory.ListIn() : OperatorHandlerProviderFactory.ListOut();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityViewOperatorTest>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);
        var keyword = isIn ? "in" : "out";

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Operators().AsQueryable(), $"{keyword}(id,(13,14,15))");

        // Assert
        Assert.True(actualResult.IsError);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(isIn ? RqlOperators.ListIn : RqlOperators.ListOut), actualResult.FirstError.Description);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WithRqlLike_OperatorProhibited(bool insensitive)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlLike("id", "13*", insensitive);
        var operatorHandlerProviderMock = insensitive ? OperatorHandlerProviderFactory.ILike() : OperatorHandlerProviderFactory.Like();
        var pathBuilder = PathBuilderFactory.Internal();
        var binaryExpressionBuilder = BinaryExpressionBuilderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, pathBuilder, binaryExpressionBuilder, parserMock);
        var keyword = insensitive ? "ilike" : "like";

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Operators().AsQueryable(), $"{keyword}(id,13*)");

        // Assert
        Assert.True(actualResult.IsError);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(RqlOperators.StartsWith), actualResult.FirstError.Description);
    }
}