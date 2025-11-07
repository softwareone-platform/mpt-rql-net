using SoftwareOne.Rql;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.UnitTests.Common;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests;

public class NullabilityIntegrationTests
{
    [Fact]
    public void NullabilityOperators_ShouldBeAutomaticallySet()
    {
        // Arrange
        var provider = MetadataProviderFactory.Public();

        // Act
        var properties = provider.GetPropertiesByDeclaringType(typeof(NullabilityTestCases));

        // Assert - Nullable value types should have Null operator
        var nullableInt = properties.Single(p => p.Name == "nullableInt");
        Assert.True((nullableInt.Operators & RqlOperators.Null) == RqlOperators.Null);

        // Non-nullable value types should NOT have Null operator
        var regularInt = properties.Single(p => p.Name == "regularInt");
        Assert.True((regularInt.Operators & RqlOperators.Null) == RqlOperators.None);

        // Nullable reference types should have proper nullability detection 
        var nullableString = properties.Single(p => p.Name == "nullableString");
        Assert.True(nullableString.IsNullable);

        // Non-nullable reference types should not be nullable
        var regularString = properties.Single(p => p.Name == "regularString");
        Assert.False(regularString.IsNullable);
    }

    [Fact]
    public void AutomaticNullability_ShouldOverrideObsoleteAttribute()
    {
        // Arrange
        var provider = MetadataProviderFactory.Public();

        // Act
        var properties = provider.GetPropertiesByDeclaringType(typeof(ObsoleteAttributeTestCases));

        // Assert
        var stringProp = properties.Single(p => p.Name == "testProperty");

        // Even though the attribute says IsNullable = false, 
        // the automatic detection should correctly identify it as nullable
        Assert.True(stringProp.IsNullable);
    }
}

public class NullabilityTestCases
{
    public int? NullableInt { get; set; }
    public int RegularInt { get; set; }
    public string? NullableString { get; set; }
    public string RegularString { get; set; } = null!;
}

public class ObsoleteAttributeTestCases
{
#pragma warning disable CS0618 // Type or member is obsolete
    [RqlProperty(IsNullable = false)]
    public string? TestProperty { get; set; }
#pragma warning restore CS0618 // Type or member is obsolete
}