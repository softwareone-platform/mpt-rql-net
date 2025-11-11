using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Rql.Tests.Common.Factory;
using Rql.Tests.Common.Utility;
using Xunit;

namespace Rql.Tests.Unit;

public class NullabilityValidationTests
{
    [Fact]
    public void SampleEntity_NullabilityDetection_ShouldWorkCorrectly()
    {
        // Arrange
        var provider = MetadataProviderFactory.Public();

        // Act
        var properties = provider.GetPropertiesByDeclaringType(typeof(SampleEntity));

        // Assert - Verify that nullability is automatically detected
        
        // MakeFlag is nullable bool (bool?)
        var makeFlagProp = properties.Single(p => p.Name == "makeFlag");
        Assert.True(makeFlagProp.IsNullable);

        // FinishedGoodsFlag is nullable bool (bool?)
        var finishedGoodsFlagProp = properties.Single(p => p.Name == "finishedGoodsFlag");
        Assert.True(finishedGoodsFlagProp.IsNullable);

        // Size is nullable string (string?)
        var sizeProp = properties.Single(p => p.Name == "size");
        Assert.True(sizeProp.IsNullable);

        // Weight is nullable decimal (decimal?)
        var weightProp = properties.Single(p => p.Name == "weight");
        Assert.True(weightProp.IsNullable);

        // Types is nullable collection
        var typesProp = properties.Single(p => p.Name == "types");
        Assert.True(typesProp.IsNullable);

        // Category is NOT nullable (has null! annotation)
        var categoryProp = properties.Single(p => p.Name == "category");
        Assert.False(categoryProp.IsNullable);

        // ProductName is NOT nullable (has null! annotation)
        var productNameProp = properties.Single(p => p.Name == "productName");
        Assert.False(productNameProp.IsNullable);

        // Id is NOT nullable (int)
        var idProp = properties.Single(p => p.Name == "id");
        Assert.False(idProp.IsNullable);

        // StandardCost is NOT nullable (decimal)
        var standardCostProp = properties.Single(p => p.Name == "standardCost");
        Assert.False(standardCostProp.IsNullable);
    }

    [Fact]
    public void MetadataOperatorTestEntity_NullOperators_ShouldBeSetCorrectly()
    {
        // Arrange
        var provider = MetadataProviderFactory.Public();

        // Act  
        var properties = provider.GetPropertiesByDeclaringType(typeof(MetadataOperatorTestEntity));

        // Assert
        
        // Nullable int should have Null operators
        var nullableIntProp = properties.Single(p => p.Name == "genericNullableDefaults");
        Assert.True((nullableIntProp.Operators & RqlOperators.Null) == RqlOperators.Null);

        // Regular int should NOT have Null operators
        var regularIntProp = properties.Single(p => p.Name == "genericDefaults");
        Assert.True((regularIntProp.Operators & RqlOperators.Null) == RqlOperators.None);

        // Nullable GUID should have Null operators
        var nullableGuidProp = properties.Single(p => p.Name == "nullableGuidDefaults");
        Assert.True((nullableGuidProp.Operators & RqlOperators.Null) == RqlOperators.Null);

        // Regular GUID should NOT have Null operators
        var regularGuidProp = properties.Single(p => p.Name == "guidDefaults");
        Assert.True((regularGuidProp.Operators & RqlOperators.Null) == RqlOperators.None);

        // String properties have Null operators by default (part of StringDefaults)
        var stringProp = properties.Single(p => p.Name == "stringDefaults");
        Assert.True((stringProp.Operators & RqlOperators.Null) == RqlOperators.Null);
    }

    [Fact]
    public void AutomaticNullability_ShouldReplaceObsoleteIsNullableAttribute()
    {
        // This test ensures that the new automatic nullability detection
        // works correctly and the old IsNullable attribute is obsolete
        
        // Arrange
        var provider = MetadataProviderFactory.Public();

        // Act
        var properties = provider.GetPropertiesByDeclaringType(typeof(SampleEntity));

        // Assert - These should be detected automatically based on type annotations
        // rather than relying on manual IsNullable attributes

        // Nullable value types should be detected as nullable
        var nullableBool = properties.Single(p => p.Name == "makeFlag");
        Assert.True(nullableBool.IsNullable);

        var nullableDecimal = properties.Single(p => p.Name == "weight");
        Assert.True(nullableDecimal.IsNullable);

        // Non-nullable value types should NOT be nullable
        var regularInt = properties.Single(p => p.Name == "id");
        Assert.False(regularInt.IsNullable);

        var regularDecimal = properties.Single(p => p.Name == "standardCost");
        Assert.False(regularDecimal.IsNullable);
    }
}