﻿using Moq;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation;
using SoftwareOne.UnitTests.Common;
using SoftwareOne.UnitTests.Common.Utility;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Filtering;

public class FilteringServiceTests
{
    private static (FilteringService<TView> sut, QueryContext<TView> context) BuildSut<TView, TOperator>(IRqlParser parserMock)
        where TOperator : IOperator, new()
    {
        var contextSubstitute = new QueryContext<TView>();
        var graphBuilder = new Mock<IFilteringGraphBuilder<TView>>();
        var builder = ExpressionBuilderFactory.GetBinary<TOperator>();
        var sut = new FilteringService<TView>(contextSubstitute, graphBuilder.Object, builder, parserMock);
        return (sut, contextSubstitute);
    }

    [Theory]
    [InlineData("eq(name,Jewelry Widget)")]
    [InlineData("name=Jewelry Widget")]
    [InlineData("eq(sub.name,Jewelry Widget)")]
    [InlineData("sub.name=Jewelry Widget")]
    public void Apply_WithRqlEqual_ReturnsSingleFilteredResult(string query)
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView, Equal>(RqlParserFactory.RqlEqual());

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    [Fact]
    public void Apply_WithRqlNotEqual_ReturnsAllButSingleResult()
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView, NotEqual>(RqlParserFactory.RqlNotEqual());

        // Act
        sut.Process("ne(name,Jewelry Widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.False(actualResult.Any(_ => _.Name == "Jewelry Widget"));
    }

    [Theory]
    [InlineData("gt(price,129.99)", 129.99)]
    [InlineData("gt(price,200)", 200)]
    public void Apply_WithRqlGreaterThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView, GreaterThan>(RqlParserFactory.RqlGreaterThan(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

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
        var (sut, context) = BuildSut<SampleEntityView, GreaterThan>(RqlParserFactory.RqlGreaterThan(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("ge(price,129.99)", 129.99)]
    [InlineData("ge(price,200)", 200)]
    public void Apply_WithRqlGreaterEqualThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView, GreaterThanOrEqual>(RqlParserFactory.RqlGreaterThanOrEqual(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

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
        var (sut, context) = BuildSut<SampleEntityView, GreaterThanOrEqual>(RqlParserFactory.RqlGreaterThanOrEqual(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("lt(price,150.1)", 150.1)]
    [InlineData("lt(price,500)", 500)]
    public void Apply_WithRqlLessThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView, LessThan>(RqlParserFactory.RqlLessThan(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

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
        var (sut, context) = BuildSut<SampleEntityView, LessThan>(RqlParserFactory.RqlLessThan(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("le(price,150.1)", 150.1)]
    [InlineData("le(price,500)", 500)]
    public void Apply_WithRqlLessEqualThan_ReturnsExpectedResult(string query, decimal value)
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView, LessThanOrEqual>(RqlParserFactory.RqlLessThanOrEqual(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

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
        var (sut, context) = BuildSut<SampleEntityView, LessThanOrEqual>(RqlParserFactory.RqlLessThanOrEqual(value));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Theory]
    [InlineData("like(name,*Widget)", "*Widget", 1)]
    [InlineData("like(name,*DOES_NOT_EXIST)", "*DOES_NOT_EXIST", 0)]
    public void Apply_WithRqlLikeMatch_ReturnsExpectedResult(string query, string searchString, int expectedCount)
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView, Like>(RqlParserFactory.RqlLike(searchString));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

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
        var (sut, context) = BuildSut<SampleEntityView, Like>(RqlParserFactory.RqlLike(searchString));

        // Act
        sut.Process(query);
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Equal(actualResult.Count(), expectedCount);
    }

    [Fact]
    public void Apply_WithRqlEqual_OperatorProhibited()
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityViewOperatorTest, Equal>(RqlParserFactory.RqlEqual("id", "13"));

        // Act
        sut.Process("id=13");
        var errors = context.GetErrors();

        // Assert
        Assert.True(context.HasErrors);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(RqlOperators.Eq), errors.First().Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WithRqlList_OperatorProhibited(bool isIn)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlList(isIn, "id", "13", "14", "15");
        var (sut, context) = isIn
            ? BuildSut<SampleEntityViewOperatorTest, ListIn>(parserMock)
            : BuildSut<SampleEntityViewOperatorTest, ListOut>(parserMock);
        var keyword = isIn ? "in" : "out";

        // Act
        sut.Process($"{keyword}(id,(13,14,15))");
        var errors = context.GetErrors();

        // Assert
        Assert.True(context.HasErrors);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(isIn ? RqlOperators.ListIn : RqlOperators.ListOut), errors.First().Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WithRqlLike_OperatorProhibited(bool insensitive)
    {
        // Arrange
        var parserMock = RqlParserFactory.RqlLike("id", "13*", insensitive);
        var (sut, context) = insensitive
            ? BuildSut<SampleEntityView, LikeInsensitive>(parserMock)
            : BuildSut<SampleEntityView, Like>(parserMock);
        var keyword = insensitive ? "ilike" : "like";

        // Act
        sut.Process($"{keyword}(id,13*)");
        var errors = context.GetErrors();

        // Assert
        Assert.True(context.HasErrors);
        Assert.Equal(ErrorMessageFactory.OperatorProhibited(RqlOperators.StartsWith), errors.First().Message);
    }
}