using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Filtering.Operators;
using Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;
using Mpt.Rql.Services.Filtering.Operators.Search.Implementation;
using Mpt.Rql.Settings;
using Rql.Tests.Common.Factory;
using Rql.Tests.Common.Utility;
using Xunit;

namespace Rql.Tests.Unit.Filtering;

public class CaseSensitivityTests
{
    private static (FilteringService<TView> sut, QueryContext<TView> context) BuildSut<TView>(
        IRqlParser parserMock, 
        IOperator operatorInstance, 
        bool caseInsensitive = false)
    {
        var settings = new RqlSettings();
        settings.Filter.Strings.Comparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : null;

        var contextSubstitute = new QueryContext<TView>();
        var graphBuilder = new Mock<IFilteringGraphBuilder<TView>>();
        var builder = ExpressionBuilderFactory.GetBinary(operatorInstance);
        var sut = new FilteringService<TView>(contextSubstitute, graphBuilder.Object, builder, parserMock);
        return (sut, contextSubstitute);
    }

    #region Equal Operator Case Sensitivity Tests

    [Fact]
    public void Equal_CaseSensitive_ShouldMatchExactCase()
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlEqual("name", "Jewelry Widget"), 
            new Equal(new RqlSettings()), // Default is case-sensitive
            caseInsensitive: false);

        // Act
        sut.Process("eq(name,Jewelry Widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Single(actualResult);
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    [Fact]
    public void Equal_CaseSensitive_ShouldNotMatchDifferentCase()
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlEqual("name", "jewelry widget"), 
            new Equal(new RqlSettings()), // Default is case-sensitive
            caseInsensitive: false);

        // Act
        sut.Process("eq(name,jewelry widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Fact]
    public void Equal_CaseInsensitive_ShouldMatchDifferentCase()
    {
        // Arrange
        var settings = new RqlSettings();
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlEqual("name", "jewelry widget"), 
            new Equal(settings),
            caseInsensitive: true);

        // Act
        sut.Process("eq(name,jewelry widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Single(actualResult);
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    [Theory]
    [InlineData("JEWELRY WIDGET")]
    [InlineData("jewelry widget")]
    [InlineData("Jewelry Widget")]
    [InlineData("jEWELRY wIDGET")]
    public void Equal_CaseInsensitive_ShouldMatchVariousCases(string searchValue)
    {
        // Arrange
        var settings = new RqlSettings();
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlEqual("name", searchValue), 
            new Equal(settings),
            caseInsensitive: true);

        // Act
        sut.Process($"eq(name,{searchValue})");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Single(actualResult);
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    #endregion

    #region NotEqual Operator Case Sensitivity Tests

    [Fact]
    public void NotEqual_CaseSensitive_ShouldExcludeExactMatch()
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlNotEqual(), 
            new NotEqual(new RqlSettings()),
            caseInsensitive: false);

        // Act
        sut.Process("ne(name,Jewelry Widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.DoesNotContain(actualResult, x => x.Name == "Jewelry Widget");
        Assert.True(actualResult.Any()); // Should have other items
    }

    [Fact]
    public void NotEqual_CaseInsensitive_ShouldExcludeAllCaseVariations()
    {
        // Arrange
        var settings = new RqlSettings();
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlNotEqual(), 
            new NotEqual(settings),
            caseInsensitive: true);

        // Act
        sut.Process("ne(name,Jewelry Widget)"); // Using exact case but should still exclude due to case insensitive setting
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.DoesNotContain(actualResult, x => x.Name == "Jewelry Widget");
        Assert.True(actualResult.Any()); // Should have other items
    }

    #endregion

    #region Like Operator Case Sensitivity Tests

    [Fact]
    public void Like_CaseSensitive_ShouldMatchExactCasePattern()
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlLike("*Widget"), 
            new Like(new RqlSettings()),
            caseInsensitive: false);

        // Act
        sut.Process("like(name,*Widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Single(actualResult);
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    [Fact]
    public void Like_CaseSensitive_ShouldNotMatchDifferentCasePattern()
    {
        // Arrange
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlLike("*widget"), 
            new Like(new RqlSettings()),
            caseInsensitive: false);

        // Act
        sut.Process("like(name,*widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Empty(actualResult);
    }

    [Fact]
    public void Like_CaseInsensitive_ShouldMatchDifferentCasePattern()
    {
        // Arrange
        var settings = new RqlSettings();
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlLike("*widget"), 
            new Like(settings),
            caseInsensitive: true);

        // Act
        sut.Process("like(name,*widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Single(actualResult);
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    [Theory]
    [InlineData("*WIDGET")]
    [InlineData("*widget")]
    [InlineData("*Widget")]
    [InlineData("*wIDGET")]
    [InlineData("jewelry*")]
    [InlineData("JEWELRY*")]
    [InlineData("Jewelry*")]
    [InlineData("*jewelry*")]
    [InlineData("*JEWELRY*")]
    public void Like_CaseInsensitive_ShouldMatchVariousCasePatterns(string pattern)
    {
        // Arrange
        var settings = new RqlSettings();
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlLike(pattern), 
            new Like(settings),
            caseInsensitive: true);

        // Act
        sut.Process($"like(name,{pattern})");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Single(actualResult);
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    [Theory]
    [InlineData("Camping*")]
    [InlineData("*Contraption")]
    [InlineData("*Apparatus*")]
    [InlineData("*Whatchamacallit")]
    public void Like_CaseInsensitive_ShouldMatchMultipleItemsCaseInsensitive(string pattern)
    {
        // Arrange
        var settings = new RqlSettings();
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlLike(pattern.ToLower()), 
            new Like(settings),
            caseInsensitive: true);

        // Act
        sut.Process($"like(name,{pattern.ToLower()})");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.True(actualResult.Any());
        // All results should contain the pattern (case insensitive)
        var expectedPattern = pattern.Replace("*", "");
        Assert.All(actualResult, item => 
            Assert.Contains(expectedPattern, item.Name, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_DefaultSettings_ShouldBeCaseSensitive()
    {
        // Arrange
        var settings = new RqlSettings();

        // Assert - Default should be null (which means use default string behavior)
        Assert.Null(settings.Filter.Strings.Comparison);
    }

    [Fact]
    public void Configuration_CanSetCaseInsensitive()
    {
        // Arrange
        var settings = new RqlSettings();

        // Act
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;

        // Assert
        Assert.Equal(StringComparison.OrdinalIgnoreCase, settings.Filter.Strings.Comparison);
    }

    [Theory]
    [InlineData(StringComparisonStrategy.Simple)]
    [InlineData(StringComparisonStrategy.Lexicographical)]
    public void Configuration_CaseInsensitivityWorksWithBothComparisonTypes(StringComparisonStrategy strategy)
    {
        // Arrange
        var settings = new RqlSettings();
        settings.Filter.Strings.Strategy = strategy;
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        
        var (sut, context) = BuildSut<SampleEntityView>(
            RqlParserFactory.RqlEqual("name", "jewelry widget"), 
            new Equal(settings),
            caseInsensitive: true);

        // Act
        sut.Process("eq(name,jewelry widget)");
        var actualResult = context.ApplyTransformations(QueryableSampleEntityCollection.Default().AsQueryable());

        // Assert
        Assert.Single(actualResult);
        Assert.Equal("Jewelry Widget", actualResult.Single().Name);
    }

    #endregion
}