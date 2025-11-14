using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Settings;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Unit;

public class NullabilityTests
{
    private readonly IRqlMetadataProvider _provider;
    private readonly MetadataFactory _metadataFactory;

    public NullabilityTests()
    {
        _metadataFactory = new MetadataFactory(new GlobalRqlSettings());
        _provider = new MetadataProvider(new PropertyNameProvider(), _metadataFactory);
    }

    [Theory]
    [InlineData(nameof(NullabilityTestEntity.NullableInt), true)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableInt), false)]
    [InlineData(nameof(NullabilityTestEntity.NullableDateTime), true)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableDateTime), false)]
    [InlineData(nameof(NullabilityTestEntity.NullableGuid), true)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableGuid), false)]
    [InlineData(nameof(NullabilityTestEntity.NullableDecimal), true)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableDecimal), false)]
    public void NullabilityDetection_ValueTypes_ShouldDetectCorrectly(string propertyName, bool expectedNullable)
    {
        // Arrange
        var property = typeof(NullabilityTestEntity).GetProperty(propertyName)!;

        // Act
        var propertyInfo = _metadataFactory.MakeRqlPropertyInfo(propertyName, property);

        // Assert
        Assert.Equal(expectedNullable, propertyInfo.IsNullable);
    }

    [Theory]
    [InlineData(nameof(NullabilityTestEntity.NullableString), true)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableString), false)]
    [InlineData(nameof(NullabilityTestEntity.NullableObject), true)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableObject), false)]
    [InlineData(nameof(NullabilityTestEntity.NullableList), true)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableList), false)]
    public void NullabilityDetection_ReferenceTypes_ShouldDetectCorrectly(string propertyName, bool expectedNullable)
    {
        // Arrange
        var property = typeof(NullabilityTestEntity).GetProperty(propertyName)!;

        // Act
        var propertyInfo = _metadataFactory.MakeRqlPropertyInfo(propertyName, property);

        // Assert
        Assert.Equal(expectedNullable, propertyInfo.IsNullable);
    }

    [Theory]
    [InlineData(nameof(NullabilityTestEntity.NullableInt), RqlOperators.GenericDefaults | RqlOperators.Null)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableInt), RqlOperators.GenericDefaults)]
    [InlineData(nameof(NullabilityTestEntity.NullableGuid), RqlOperators.GuidDefaults | RqlOperators.Null)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableGuid), RqlOperators.GuidDefaults)]
    [InlineData(nameof(NullabilityTestEntity.NullableString), RqlOperators.StringDefaults)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableString), RqlOperators.StringDefaults)]
    public void NullabilityDetection_ShouldAffectOperators(string propertyName, RqlOperators expectedOperators)
    {
        // Arrange
        var properties = _provider.GetPropertiesByDeclaringType(typeof(NullabilityTestEntity));

        // Act
        var property = properties.Single(p => p.Name == JsonNamingPolicy.CamelCase.ConvertName(propertyName));

        // Assert
        Assert.Equal(expectedOperators, property.Operators);
    }

    [Fact]
    public void IsNullableAttribute_ShouldOverrideAutomaticDetection()
    {
        // Arrange
#pragma warning disable CS0618 // Type or member is obsolete
        var property = typeof(NullabilityTestEntity).GetProperty(nameof(NullabilityTestEntity.NullableAttributeProperty))!;

        // Act
        var propertyInfo = _metadataFactory.MakeRqlPropertyInfo(nameof(NullabilityTestEntity.NullableAttributeProperty), property);
#pragma warning restore CS0618 // Type or member is obsolete

        // Assert  
        // The IsNullable=true attribute should override automatic detection
        Assert.True(propertyInfo.IsNullable);
    }

    [Theory]
    [InlineData(nameof(NullabilityTestEntity.NonNullableAttributeProperty), true)]
    [InlineData(nameof(NullabilityTestEntity.NullableStringMadeNonNullable), false)]
    [InlineData(nameof(NullabilityTestEntity.NonNullableIntMadeNullable), true)]
    [InlineData(nameof(NullabilityTestEntity.NullableIntMadeNonNullable), false)]
    public void IsNullableAttribute_ShouldOverrideAutomaticDetectionForAllTypes(string propertyName, bool expectedNullable)
    {
        // Arrange
        var property = typeof(NullabilityTestEntity).GetProperty(propertyName)!;

        // Act
        var propertyInfo = _metadataFactory.MakeRqlPropertyInfo(propertyName, property);

        // Assert
        Assert.Equal(expectedNullable, propertyInfo.IsNullable);
    }

    [Fact]
    public void NullabilityDetection_ComplexTypes_ShouldDetectCorrectly()
    {
        // Arrange
        var properties = _provider.GetPropertiesByDeclaringType(typeof(NullabilityTestEntity));

        // Act & Assert
        var nullableComplexProperty = properties.Single(p => p.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(NullabilityTestEntity.NullableComplexObject)));
        var nonNullableComplexProperty = properties.Single(p => p.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(NullabilityTestEntity.NonNullableComplexObject)));

        Assert.True(nullableComplexProperty.IsNullable);
        Assert.False(nonNullableComplexProperty.IsNullable);
    }

    [Fact]
    public void NullabilityDetection_ShouldWorkWithMetadataProvider()
    {
        // Arrange & Act
        var properties = _provider.GetPropertiesByDeclaringType(typeof(NullabilityTestEntity));

        // Assert - Check various nullable properties
        Assert.Contains(properties, p => p.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(NullabilityTestEntity.NullableInt)) && p.IsNullable);
        Assert.Contains(properties, p => p.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(NullabilityTestEntity.NonNullableInt)) && !p.IsNullable);
        Assert.Contains(properties, p => p.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(NullabilityTestEntity.NullableString)) && p.IsNullable);
        Assert.Contains(properties, p => p.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(NullabilityTestEntity.NonNullableString)) && !p.IsNullable);
    }
}

public class NullabilityTestEntity
{
    // Value types
    public int? NullableInt { get; set; }
    public int NonNullableInt { get; set; }
    public DateTime? NullableDateTime { get; set; }
    public DateTime NonNullableDateTime { get; set; }
    public Guid? NullableGuid { get; set; }
    public Guid NonNullableGuid { get; set; }
    public decimal? NullableDecimal { get; set; }
    public decimal NonNullableDecimal { get; set; }

    // Reference types - nullable annotations
    public string? NullableString { get; set; }
    public string NonNullableString { get; set; } = null!;
    public object? NullableObject { get; set; }
    public object NonNullableObject { get; set; } = null!;
    public List<string>? NullableList { get; set; }
    public List<string> NonNullableList { get; set; } = null!;

    // Complex types
    public NullabilityComplexType? NullableComplexObject { get; set; }

    public NullabilityComplexType NonNullableComplexObject { get; set; } = null!;

    // Test IsNullable attribute override functionality
    [RqlProperty(IsNullable = true)]
    public string? NullableAttributeProperty { get; set; }

    // Test override scenarios
    [RqlProperty(IsNullable = true)]
    public string NonNullableAttributeProperty { get; set; } = null!;

    [RqlProperty(IsNullable = false)]
    public string? NullableStringMadeNonNullable { get; set; }

    [RqlProperty(IsNullable = true)]
    public int NonNullableIntMadeNullable { get; set; }

    [RqlProperty(IsNullable = false)]
    public int? NullableIntMadeNonNullable { get; set; }
}

public class NullabilityComplexType
{
    public string Name { get; set; } = null!;
    public int Value { get; set; }
}