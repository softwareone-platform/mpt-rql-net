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
        Assert.Equal(StringComparisonType.Simple, settings.ComparisonType);
        Assert.False(settings.CaseInsensitive);
    }

    [Fact]
    public void RqlStringFilterSettings_CanSetComparisonType()
    {
        // Arrange
        var settings = new RqlStringFilterSettings();

        // Act
        settings.ComparisonType = StringComparisonType.Lexicographical;

        // Assert
        Assert.Equal(StringComparisonType.Lexicographical, settings.ComparisonType);
    }

    [Fact]
    public void RqlStringFilterSettings_CanSetCaseInsensitive()
    {
        // Arrange
        var settings = new RqlStringFilterSettings();

        // Act
        settings.CaseInsensitive = true;

        // Assert
        Assert.True(settings.CaseInsensitive);
    }

    [Theory]
    [InlineData(StringComparisonType.Simple, true)]
    [InlineData(StringComparisonType.Simple, false)]
    [InlineData(StringComparisonType.Lexicographical, true)]
    [InlineData(StringComparisonType.Lexicographical, false)]
    public void RqlStringFilterSettings_CanSetAllCombinations(StringComparisonType comparisonType, bool caseInsensitive)
    {
        // Arrange
        var settings = new RqlStringFilterSettings();

        // Act
        settings.ComparisonType = comparisonType;
        settings.CaseInsensitive = caseInsensitive;

        // Assert
        Assert.Equal(comparisonType, settings.ComparisonType);
        Assert.Equal(caseInsensitive, settings.CaseInsensitive);
    }

    [Fact]
    public void GlobalRqlSettings_DefaultStringSettings_ShouldBeCorrect()
    {
        // Arrange & Act
        var globalSettings = new GlobalRqlSettings();

        // Assert
        Assert.Equal(StringComparisonType.Simple, globalSettings.Filter.Strings.ComparisonType);
        Assert.False(globalSettings.Filter.Strings.CaseInsensitive);
    }

    [Fact]
    public void GlobalRqlSettings_CanModifyStringSettings()
    {
        // Arrange
        var globalSettings = new GlobalRqlSettings();

        // Act
        globalSettings.Filter.Strings.ComparisonType = StringComparisonType.Lexicographical;
        globalSettings.Filter.Strings.CaseInsensitive = true;

        // Assert
        Assert.Equal(StringComparisonType.Lexicographical, globalSettings.Filter.Strings.ComparisonType);
        Assert.True(globalSettings.Filter.Strings.CaseInsensitive);
    }

    [Fact]
    public void RqlSettings_DefaultStringSettings_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = new RqlSettings();

        // Assert
        Assert.Equal(StringComparisonType.Simple, settings.Filter.Strings.ComparisonType);
        Assert.False(settings.Filter.Strings.CaseInsensitive);
    }

    [Fact]
    public void RqlSettings_CanModifyStringSettings()
    {
        // Arrange
        var settings = new RqlSettings();

        // Act
        settings.Filter.Strings.ComparisonType = StringComparisonType.Lexicographical;
        settings.Filter.Strings.CaseInsensitive = true;

        // Assert
        Assert.Equal(StringComparisonType.Lexicographical, settings.Filter.Strings.ComparisonType);
        Assert.True(settings.Filter.Strings.CaseInsensitive);
    }
}

public class StringComparisonTypeTests
{
    [Fact]
    public void StringComparisonType_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)StringComparisonType.Simple);
        Assert.Equal(1, (int)StringComparisonType.Lexicographical);
    }

    [Theory]
    [InlineData(StringComparisonType.Simple)]
    [InlineData(StringComparisonType.Lexicographical)]
    public void StringComparisonType_AllValues_ShouldBeValidEnumValues(StringComparisonType value)
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(StringComparisonType), value));
    }

    [Fact]
    public void StringComparisonType_ShouldHaveCorrectNames()
    {
        // Assert
        Assert.Equal("Simple", Enum.GetName(typeof(StringComparisonType), StringComparisonType.Simple));
        Assert.Equal("Lexicographical", Enum.GetName(typeof(StringComparisonType), StringComparisonType.Lexicographical));
    }
}