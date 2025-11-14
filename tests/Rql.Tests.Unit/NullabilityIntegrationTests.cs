using Mpt.Rql;
using Rql.Tests.Common.Factory;
using Xunit;

namespace Rql.Tests.Unit;

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
    public void IsNullableAttribute_ShouldOverrideAutomaticDetection()
    {
        // Arrange
        var provider = MetadataProviderFactory.Public();

        // Act
        var properties = provider.GetPropertiesByDeclaringType(typeof(AttributeOverrideTestCases));

        // Assert
        var stringProp = properties.Single(p => p.Name == "testProperty");

        // The attribute explicitly sets IsNullable = false, which should override 
        // the automatic detection (which would normally detect string? as nullable)
        Assert.False(stringProp.IsNullable);
    }
}

public class NullabilityTestCases
{
    public int? NullableInt { get; set; }
    public int RegularInt { get; set; }
    public string? NullableString { get; set; }
    public string RegularString { get; set; } = null!;
}

public class AttributeOverrideTestCases
{
    [RqlProperty(IsNullable = false)]
    public string? TestProperty { get; set; }
}