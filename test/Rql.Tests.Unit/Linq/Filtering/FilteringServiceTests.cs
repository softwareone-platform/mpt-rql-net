using Rql.Tests.Unit.Factory;
using Rql.Tests.Unit.Utility;
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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

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
        var typeMetadataProvider = TypeMetadataProviderFactory.Internal();
        var sut = new FilteringService<SampleEntityView>(operatorHandlerProviderMock, typeMetadataProvider, parserMock);

        // Act
        var actualResult = sut.Apply(QueryableSampleEntityCollection.Default().AsQueryable(), query);

        // Assert
        Assert.False(actualResult.IsError);
        Assert.Equal(actualResult.Value.Count(), expectedCount);
    }
}