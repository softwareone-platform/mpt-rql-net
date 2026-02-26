using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Integration tests for SafeNavigation in mapping operations.
/// Verifies that Mapping.Navigation setting prevents null reference exceptions
/// when projecting from storage to view models with nested properties.
/// </summary>
public class SafeNavigationMappingTests
{
    [Fact]
    public void MappingNavigation_Safe_NestedPropertyMapping_HandlesNulls()
    {
        // Arrange
        var rql = CreateRql(NavigationStrategy.Safe);
        var data = CreateDataWithNullReferences();

        // Act - Should not throw despite null references in nested properties
        var result = rql.Transform(data.AsQueryable(), new RqlRequest 
        { 
            Select = "id,name,reference.name,reference.price"
        }).Query.ToList();

        // Assert - Should return all items, nulls handled gracefully
        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Name == "HasValidReference");
        Assert.Contains(result, p => p.Name == "HasNullReference");
    }

    [Fact]
    public void MappingNavigation_Default_NestedPropertyMapping_Throws()
    {
        // Arrange
        var rql = CreateRql(NavigationStrategy.Default);
        var data = CreateDataWithNullReferences();

        // Act & Assert - Should throw when accessing null reference properties
        Assert.Throws<NullReferenceException>(() =>
            rql.Transform(data.AsQueryable(), new RqlRequest 
            { 
                Select = "id,name,reference.name,reference.price"
            }).Query.ToList());
    }

    [Fact]
    public void MappingNavigation_Safe_DeepNesting_HandlesNulls()
    {
        // Arrange
        var rql = CreateRql(NavigationStrategy.Safe);
        var data = CreateDeepNestedData();

        // Act - Should handle multiple levels of nesting with nulls
        var result = rql.Transform(data.AsQueryable(), new RqlRequest 
        { 
            Select = "id,name,reference.reference.name" 
        }).Query.ToList();

        // Assert
        Assert.Equal(3, result.Count);
        var fullChain = result.First(p => p.Name == "FullChain");
        Assert.NotNull(fullChain.Reference);
        Assert.NotNull(fullChain.Reference!.Reference);
        Assert.Equal("DeepValid", fullChain.Reference.Reference!.Name);
        
        var nullAtLevel1 = result.First(p => p.Name == "NullAtLevel1");
        Assert.Null(nullAtLevel1.Reference);
    }

    [Fact]
    public void MappingNavigation_Default_DeepNesting_Throws()
    {
        // Arrange
        var rql = CreateRql(NavigationStrategy.Default);
        var data = CreateDeepNestedData();

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            rql.Transform(data.AsQueryable(), new RqlRequest 
            { 
                Select = "id,name,reference.reference.name" 
            }).Query.ToList());
    }

    [Fact]
    public void MappingNavigation_Safe_CollectionMapping_HandlesNulls()
    {
        // Arrange
        var rql = CreateRql(NavigationStrategy.Safe);
        var data = CreateDataWithNullCollections();

        // Act - Should handle null collections gracefully
        var result = rql.Transform(data.AsQueryable(), new RqlRequest 
        { 
            Select = "id,name,tags.value" 
        }).Query.ToList();

        // Assert
        Assert.Equal(3, result.Count);
        var withTags = result.First(p => p.Name == "HasTags");
        Assert.NotNull(withTags.Tags);
        Assert.Equal(2, withTags.Tags.Count);
        
        var withNullTags = result.First(p => p.Name == "HasNullTags");
        Assert.Null(withNullTags.Tags);
    }

    [Fact]
    public void MappingNavigation_Default_CollectionMapping_Throws()
    {
        // Arrange
        var rql = CreateRql(NavigationStrategy.Default);
        var data = CreateDataWithNullCollections();

        // Act & Assert
        Assert.ThrowsAny<Exception>(() =>
            rql.Transform(data.AsQueryable(), new RqlRequest 
            { 
                Select = "id,name,tags(value)" 
            }).Query.ToList());
    }

    [Fact]
    public void MappingNavigation_Safe_MixedNullAndValid_HandlesCorrectly()
    {
        // Arrange
        var rql = CreateRql(NavigationStrategy.Safe);
        var data = new List<Product>
        {
            new() { Id = 1, Name = "First", Category = "Test",
                Reference = new() { Id = 10, Name = "Valid", Category = "RefTest", Price = 100m },
                Tags = [new() { Value = "Tag1" }] },
            new() { Id = 2, Name = "Second", Category = "Test",
                Reference = null!, // Null reference
                Tags = [new() { Value = "Tag2" }] }, // Valid tags
            new() { Id = 3, Name = "Third", Category = "Test",
                Reference = new() { Id = 11, Name = "Valid2", Category = "RefTest", Price = 200m },
                Tags = null! } // Null tags
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest 
        { 
            Select = "id,name,reference.name,tags.value" 
        }).Query.ToList();

        // Assert - All items should be returned with nulls handled
        Assert.Equal(3, result.Count);
        
        Assert.NotNull(result[0].Reference);
        Assert.NotNull(result[0].Tags);
        
        Assert.Null(result[1].Reference);
        Assert.NotNull(result[1].Tags);
        
        Assert.NotNull(result[2].Reference);
        Assert.Null(result[2].Tags);
    }

    [Fact]
    public void MappingNavigation_IndependentFromFilterAndOrdering()
    {
        // Arrange - Mapping safe, but filter/ordering unsafe
        var rql = CreateRqlIndependent(
            mappingNavigation: NavigationStrategy.Safe,
            filterNavigation: NavigationStrategy.Default,
            orderingNavigation: NavigationStrategy.Default);

        var data = new List<Product>
        {
            new() { Id = 1, Name = "Product1", Category = "Test",
                Reference = null!,
                Tags = null! }
        };

        // Act - Mapping should work, no filter/ordering applied
        var result = rql.Transform(data.AsQueryable(), new RqlRequest 
        { 
            Select = "id,name,reference.name" 
        }).Query.ToList();

        // Assert - Should succeed because we're only mapping
        Assert.Single(result);
        Assert.Null(result[0].Reference);
    }

    [Fact]
    public void MappingNavigation_CombinedWithFilterSafe_BothWork()
    {
        // Arrange - Both mapping and filter safe
        var rql = CreateRqlIndependent(
            mappingNavigation: NavigationStrategy.Safe,
            filterNavigation: NavigationStrategy.Safe,
            orderingNavigation: NavigationStrategy.Default);

        var data = CreateDataWithNullReferences();

        // Act - Both filtering and mapping should handle nulls
        var result = rql.Transform(data.AsQueryable(), new RqlRequest 
        { 
            Filter = "eq(name,HasValidReference)",
            Select = "id,name,reference.name,reference.name"
        }).Query.ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("HasValidReference", result[0].Name);
        Assert.NotNull(result[0].Reference);
    }

    #region Helper Methods

    private static IRqlQueryable<Product, Product> CreateRql(NavigationStrategy mappingNavigation)
        => RqlFactory.Make<Product>(services => { }, rqlConfig =>
        {
            rqlConfig.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rqlConfig.Settings.Select.Explicit = RqlSelectModes.All;
            rqlConfig.Settings.Select.MaxDepth = 10;
            rqlConfig.Settings.Mapping.Navigation = mappingNavigation;
        });

    private static IRqlQueryable<Product, Product> CreateRqlIndependent(
        NavigationStrategy mappingNavigation,
        NavigationStrategy filterNavigation,
        NavigationStrategy orderingNavigation)
        => RqlFactory.Make<Product>(services => { }, rqlConfig =>
        {
            rqlConfig.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rqlConfig.Settings.Select.Explicit = RqlSelectModes.All;
            rqlConfig.Settings.Select.MaxDepth = 10;
            rqlConfig.Settings.Mapping.Navigation = mappingNavigation;
            rqlConfig.Settings.Filter.Navigation = filterNavigation;
            rqlConfig.Settings.Ordering.Navigation = orderingNavigation;
        });

    #endregion

    #region Test Data

    private static IEnumerable<Product> CreateDataWithNullReferences()
    {
        return
        [
            new() { Id = 1, Name = "HasValidReference", Category = "Test",
                Reference = new() { Id = 10, Name = "ValidReference", Category = "RefTest", Price = 100m },
                Tags = [new() { Value = "Valid" }] },
            new() { Id = 2, Name = "HasNullReference", Category = "Test",
                Reference = null!,
                Tags = [new() { Value = "Other" }] },
            new() { Id = 3, Name = "AnotherNullReference", Category = "Test",
                Reference = null!,
                Tags = null! }
        ];
    }

    private static IEnumerable<Product> CreateDeepNestedData()
    {
        return
        [
            new() { Id = 1, Name = "FullChain", Category = "Test",
                Reference = new() { Id = 10, Name = "Level1", Category = "RefTest", Price = 50m,
                    Reference = new() { Id = 20, Name = "DeepValid", Category = "DeepTest", Price = 100m } } },
            new() { Id = 2, Name = "NullAtLevel1", Category = "Test",
                Reference = null! },
            new() { Id = 3, Name = "NullAtLevel2", Category = "Test",
                Reference = new() { Id = 11, Name = "Level1", Category = "RefTest", Price = 25m,
                    Reference = null! } }
        ];
    }

    private static IEnumerable<Product> CreateDataWithNullCollections()
    {
        return
        [
            new() { Id = 1, Name = "HasTags", Category = "Test",
                Reference = new() { Id = 10, Name = "Valid", Category = "RefTest", Price = 100m },
                Tags = [new() { Value = "Tag1" }, new() { Value = "Tag2" }] },
            new() { Id = 2, Name = "HasNullTags", Category = "Test",
                Reference = new() { Id = 11, Name = "Valid2", Category = "RefTest", Price = 200m },
                Tags = null! },
            new() { Id = 3, Name = "EmptyTags", Category = "Test",
                Reference = new() { Id = 12, Name = "Valid3", Category = "RefTest", Price = 300m },
                Tags = [] }
        ];
    }

    #endregion
}
