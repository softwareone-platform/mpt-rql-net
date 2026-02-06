using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Settings;
using Xunit;

namespace Rql.Tests.Unit.Configuration;

public class StringFilterSettingsTests
{
    [Fact]
    public void RqlStringFilterSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new RqlStringFilterSettings();

        // Assert
        Assert.Equal(StringComparisonStrategy.Default, settings.Strategy);
        Assert.Null(settings.Comparison);
    }

    [Fact]
    public void RqlStringFilterSettings_CanSetStrategy()
    {
        // Arrange
        var settings = new RqlStringFilterSettings
        {
            // Act
            Strategy = StringComparisonStrategy.Lexicographical
        };

        // Assert
        Assert.Equal(StringComparisonStrategy.Lexicographical, settings.Strategy);
    }

    [Fact]
    public void RqlStringFilterSettings_CanSetComparison()
    {
        // Arrange
        var settings = new RqlStringFilterSettings
        {
            // Act
            Comparison = StringComparison.OrdinalIgnoreCase
        };

        // Assert
        Assert.Equal(StringComparison.OrdinalIgnoreCase, settings.Comparison);
    }

    [Theory]
    [InlineData(StringComparisonStrategy.Default, null)]
    [InlineData(StringComparisonStrategy.Default, StringComparison.OrdinalIgnoreCase)]
    [InlineData(StringComparisonStrategy.Lexicographical, null)]
    [InlineData(StringComparisonStrategy.Lexicographical, StringComparison.CurrentCultureIgnoreCase)]
    public void RqlStringFilterSettings_CanSetAllCombinations(StringComparisonStrategy strategy, StringComparison? comparison)
    {
        // Arrange
        var settings = new RqlStringFilterSettings
        {
            // Act
            Strategy = strategy,
            Comparison = comparison
        };

        // Assert
        Assert.Equal(strategy, settings.Strategy);
        Assert.Equal(comparison, settings.Comparison);
    }

    [Fact]
    public void GlobalRqlSettings_DefaultStringSettings_ShouldBeCorrect()
    {
        // Arrange & Act
        var globalSettings = new GlobalRqlSettings();

        // Assert
        Assert.Equal(StringComparisonStrategy.Default, globalSettings.Filter.Strings.Strategy);
        Assert.Null(globalSettings.Filter.Strings.Comparison);
    }

    [Fact]
    public void GlobalRqlSettings_CanModifyStringSettings()
    {
        // Arrange
        var globalSettings = new GlobalRqlSettings();

        // Act
        globalSettings.Filter.Strings.Strategy = StringComparisonStrategy.Lexicographical;
        globalSettings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;

        // Assert
        Assert.Equal(StringComparisonStrategy.Lexicographical, globalSettings.Filter.Strings.Strategy);
        Assert.Equal(StringComparison.OrdinalIgnoreCase, globalSettings.Filter.Strings.Comparison);
    }

    [Fact]
    public void RqlSettings_DefaultStringSettings_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new RqlSettings();

        // Assert
        Assert.Equal(StringComparisonStrategy.Default, settings.Filter.Strings.Strategy);
        Assert.Null(settings.Filter.Strings.Comparison);
    }

    [Fact]
    public void RqlSettings_CanModifyStringSettings()
    {
        // Arrange
        var settings = new RqlSettings();

        // Act
        settings.Filter.Strings.Strategy = StringComparisonStrategy.Lexicographical;
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;

        // Assert
        Assert.Equal(StringComparisonStrategy.Lexicographical, settings.Filter.Strings.Strategy);
        Assert.Equal(StringComparison.OrdinalIgnoreCase, settings.Filter.Strings.Comparison);
    }
}

public class StringComparisonStrategyTests
{
    [Fact]
    public void StringComparisonStrategy_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)StringComparisonStrategy.Default);
        Assert.Equal(1, (int)StringComparisonStrategy.Lexicographical);
    }

    [Theory]
    [InlineData(StringComparisonStrategy.Default)]
    [InlineData(StringComparisonStrategy.Lexicographical)]
    public void StringComparisonStrategy_AllValues_ShouldBeValidEnumValues(StringComparisonStrategy value)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(StringComparisonStrategy), value));
    }

    [Fact]
    public void StringComparisonStrategy_ShouldHaveCorrectNames()
    {
        // Assert
        Assert.Equal("Default", Enum.GetName(typeof(StringComparisonStrategy), StringComparisonStrategy.Default));
        Assert.Equal("Lexicographical", Enum.GetName(typeof(StringComparisonStrategy), StringComparisonStrategy.Lexicographical));
    }
}