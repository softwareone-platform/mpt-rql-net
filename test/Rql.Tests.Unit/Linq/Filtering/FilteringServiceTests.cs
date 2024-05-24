using NSubstitute;
using Rql.Tests.Unit.Factory;
using Rql.Tests.Unit.Utility;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Filtering;
using Xunit;

namespace Rql.Tests.Unit.Linq.Filtering;

public class FilteringServiceTests
{
    private readonly QueryContext<SampleEntityView> _contextSubstitute;
    private readonly IFilteringGraphBuilder<SampleEntityView> _graphBuilder;

    public FilteringServiceTests()
    {
        _contextSubstitute = new QueryContext<SampleEntityView>();
        _graphBuilder = Substitute.For<IFilteringGraphBuilder<SampleEntityView>>();
    }

    [Theory]
    [InlineData("eq(name,Jewelry Widget)")]
    [InlineData("name=Jewelry Widget")]
    [InlineData("eq(sub.name,Jewelry Widget)")]
    [InlineData("sub.name=Jewelry Widget")]
    public void Apply_WithRqlEqual_ReturnsSingleFilteredResult(string query)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.Equal>();
        var parserMock = RqlParserFactory.RqlEqual();
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    [Fact]
    public void Apply_WithRqlNotEqual_ReturnsAllButSingleResult()
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.NotEqual>();
        var parserMock = RqlParserFactory.RqlNotEqual();
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process("ne(name,Jewelry Widget)");
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.False(actualResult.Any(_ => _.Name == "Jewelry Widget"));
    }

    [Theory]
    [InlineData("gt(price,129.99)", 129.99)]
    [InlineData("gt(price,200)", 200)]
    public void Apply_WithRqlGreaterThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThan>();
        var parserMock = RqlParserFactory.RqlGreaterThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.True(actualResult.Count() > 1);
        Assert.False(actualResult.Any(_ => _.Price <= value));
    }

    [Theory]
    [InlineData("gt(price,100000)", 100000)]
    [InlineData("gt(price,12345.67)", 12345.67)]
    public void Apply_WithRqlGreaterThanTooHigh_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThan>();
        var parserMock = RqlParserFactory.RqlGreaterThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("ge(price,129.99)", 129.99)]
    [InlineData("ge(price,200)", 200)]
    public void Apply_WithRqlGreaterEqualThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThanOrEqual>();
        var parserMock = RqlParserFactory.RqlGreaterEqualThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.True(actualResult.Count() > 1);
        Assert.False(actualResult.Any(_ => _.Price < value));
    }

    [Theory]
    [InlineData("ge(price,100000)", 100000)]
    [InlineData("ge(price,12345.67)", 12345.67)]
    public void Apply_WithRqlGreaterEqualThanTooHigh_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThanOrEqual>();
        var parserMock = RqlParserFactory.RqlGreaterEqualThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("lt(price,150.1)", 150.1)]
    [InlineData("lt(price,500)", 500)]
    public void Apply_WithRqlLessThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThan>();
        var parserMock = RqlParserFactory.RqlLessThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.True(actualResult.Count() > 1);
        Assert.False(actualResult.Any(_ => _.Price >= value));
    }

    [Theory]
    [InlineData("lt(price,1)", 1)]
    [InlineData("lt(price,-10000)", -10000)]
    public void Apply_WithRqlLessThanThanTooLow_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThan>();
        var parserMock = RqlParserFactory.RqlLessThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("le(price,150.1)", 150.1)]
    [InlineData("le(price,500)", 500)]
    public void Apply_WithRqlLessEqualThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThanOrEqual>();
        var parserMock = RqlParserFactory.RqlLessEqualThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.True(actualResult.Count() > 1);
        Assert.False(actualResult.Any(_ => _.Price > value));
    }

    [Theory]
    [InlineData("le(price,1)", 1)]
    [InlineData("le(price,-10000)", -10000)]
    public void Apply_WithRqlLessEqualThanTooLow_ReturnsEmptyResult(string query, decimal value)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThanOrEqual>();
        var parserMock = RqlParserFactory.RqlLessEqualThan(value);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("like(name,*Widget)", "*Widget", 1)]
    [InlineData("like(name,*DOES_NOT_EXIST)", "*DOES_NOT_EXIST", 0)]
    public void Apply_WithRqlLikeMatch_ReturnsExpectedResult(string query, string searchString, int expectedCount)
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.Like>();
        var parserMock = RqlParserFactory.RqlLike(searchString);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Equal(actualResult.Count(), expectedCount);
    }

    [Theory]
    [InlineData("ilike(name,*WIDGET)", "*WIDGET", 0)]
    [InlineData("ilike(name,*DOES_NOT_EXIST)", "*DOES_NOT_EXIST", 0)]
    public void Apply_WithRqlILikeMatch_ReturnsExpectedResult(string query, string searchString, int expectedCount)
    {
        // Note that 'ilike(name,*WIDGET)' test returns nothing as we are using in memory database implementation and the default .net behaviour
        // is case sensitive. Kept this test in at a unit level for clarity however

        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.Like>();
        var parserMock = RqlParserFactory.RqlLike(searchString);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);

        // Act
        sut.Process(query);
        var actualResult = _contextSubstitute.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Equal(actualResult.Count(), expectedCount);
    }


    [Fact]
    public void Apply_WithRqlEqual_OperatorProhibited()
    {
        // Arrange
        var builder = ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.Equal>();
        var parserMock = RqlParserFactory.RqlEqual("id", "13");
        var contextSubstitute = new QueryContext<SampleEntityViewOperatorTest>();
        var graphBuilder = Substitute.For<IFilteringGraphBuilder<SampleEntityViewOperatorTest>>();
        var sut = new FilteringService<SampleEntityViewOperatorTest>(contextSubstitute, graphBuilder, builder, parserMock);

        // Act
        sut.Process($"id=13");
        var errors = contextSubstitute.GetErrors();

        // Assert
        Assert.True(contextSubstitute.HasErrors);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(RqlOperators.Eq), errors.First().Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WithRqlList_OperatorProhibited(bool isIn)
    {
        // Arrange
        var builder = isIn ? ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation.ListIn>()
            : ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation.ListOut>();
        var parserMock = RqlParserFactory.RqlList(isIn, "id", "13", "14", "15");
        var contextSubstitute = new QueryContext<SampleEntityViewOperatorTest>();
        var graphBuilder = Substitute.For<IFilteringGraphBuilder<SampleEntityViewOperatorTest>>();
        var sut = new FilteringService<SampleEntityViewOperatorTest>(contextSubstitute, graphBuilder, builder, parserMock);
        var keyword = isIn ? "in" : "out";

        // Act
        sut.Process($"{keyword}(id,(13,14,15))");
        var errors = contextSubstitute.GetErrors();

        // Assert
        Assert.True(contextSubstitute.HasErrors);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(isIn ? RqlOperators.ListIn : RqlOperators.ListOut), errors.First().Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WithRqlLike_OperatorProhibited(bool insensitive)
    {
        // Arrange
        var builder = insensitive ? ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.LikeInsensitive>()
            : ExpressionBuilderFactory.GetBinary<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.Like>();
        var parserMock = RqlParserFactory.RqlLike("id", "13*", insensitive);
        var sut = new FilteringService<SampleEntityView>(_contextSubstitute, _graphBuilder, builder, parserMock);
        var keyword = insensitive ? "ilike" : "like";

        // Act
        sut.Process($"{keyword}(id,13*)");
        var errors = _contextSubstitute.GetErrors();

        // Assert
        Assert.True(_contextSubstitute.HasErrors);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(RqlOperators.StartsWith), errors.First().Message);
    }
}