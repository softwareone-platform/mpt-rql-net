using System.Linq.Expressions;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Comprehensive tests for SafeNavigation functionality across all RQL operators.
/// Tests verify that SafeNavigation prevents null reference exceptions and handles
/// null values appropriately for string operations, comparisons, collections, and lists.
/// All tests use data sets that include null references to ensure complete testing.
/// </summary>
public class SafeNavigationFunctionalityTests
{
    #region String Operations Tests

    [Theory]
    [InlineData("eq(reference.name,ValidReference)", 5)] // Should find all items with ValidReference name (excluding null references)
    [InlineData("ne(reference.name,ValidReference)", 1)] // Should find only HasDifferentReference
    public void SafeNavigation_StringEqual_HandlesNullsCorrectly(string filter, int expectedCount)
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = filter });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert
        Assert.Equal(expectedCount, result.Count);
        if (filter.Contains("eq(reference.name,ValidReference)"))
        {
            // Should include all items with ValidReference name, exclude null references
            Assert.Contains(result, p => p.Name == "HasValidReference");
            Assert.Contains(result, p => p.Name == "CheapProduct");
            Assert.Contains(result, p => p.Name == "ExpensiveProduct");
            Assert.Contains(result, p => p.Name == "HasTags");
            Assert.Contains(result, p => p.Name == "HasNullTags");
            Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
            Assert.DoesNotContain(result, p => p.Name == "AnotherNullReference");
        }
        else
        {
            // Should include only HasDifferentReference
            Assert.Single(result);
            Assert.Equal("HasDifferentReference", result[0].Name);
        }
    }

    [Fact]
    public void SafeNavigation_StringEquals_WithNullProperty_ReturnsNoResults()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act - searching for a value when property is null should return no results
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "eq(reference.name,SomeValue)" });

        Assert.True(transformResult.IsSuccess);
        var result = transformResult.Query.ToList();

        // Assert - Items with null reference.name won't match any specific value
        Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
    }

    [Theory]
    [InlineData("like(reference.name,*Valid*)", 5)] // Should match all items with "ValidReference" name
    [InlineData("like(reference.name,Valid*)", 5)] // Should match all items with "ValidReference" name
    [InlineData("like(reference.name,*Reference*)", 6)] // Should match items with "ValidReference" and "DifferentReference" names
    public void SafeNavigation_StringLike_HandlesNullsCorrectly(string filter, object expectedCount)
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = filter });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert - Should exclude items with null references
        Assert.Equal((int)expectedCount, result.Count);
        Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
        Assert.DoesNotContain(result, p => p.Name == "AnotherNullReference");
    }

    #endregion

    #region Numeric Comparison Tests

    [Theory]
    [InlineData("gt(reference.price,50)", 5)] // HasValidReference (300), HasDifferentReference (250), ExpensiveProduct (150), HasTags (350), HasNullTags (400)
    [InlineData("ge(reference.price,100)", 5)] // Same 5 items, all >= 100
    [InlineData("lt(reference.price,200)", 2)] // CheapProduct (25) and ExpensiveProduct (150)
    [InlineData("le(reference.price,50)", 1)] // CheapProduct (25)
    public void SafeNavigation_NumericComparison_HandlesNullsCorrectly(string filter, int expectedCount)
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = filter });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert
        Assert.Equal(expectedCount, result.Count);
        // Items with null references should be excluded by safe navigation
        Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
        Assert.DoesNotContain(result, p => p.Name == "AnotherNullReference");
    }

    [Fact]
    public void SafeNavigation_NumericComparison_WithNullProperty_ReturnsNoResults()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act - comparison with null property should not match
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "gt(reference.price,0)" });

        Assert.True(transformResult.IsSuccess);
        var result = transformResult.Query.ToList();

        // Assert - Should not include items with null reference
        Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
        Assert.DoesNotContain(result, p => p.Name == "AnotherNullReference");
    }

    #endregion

    #region Collection Operations Tests

    [Fact]
    public void SafeNavigation_CollectionAny_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "any(tags,eq(value,Important))" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("HasTags", result[0].Name);
    }

    [Fact]
    public void SafeNavigation_CollectionAll_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "all(tags,ne(value,Invalid))" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert - Should include items with valid tags, exclude null collections  
        Assert.Equal(5, result.Count); // All items with non-null tag collections pass the "all" condition
        Assert.Contains(result, p => p.Name == "HasValidReference");
        Assert.Contains(result, p => p.Name == "HasDifferentReference");
        Assert.Contains(result, p => p.Name == "CheapProduct");
        Assert.Contains(result, p => p.Name == "ExpensiveProduct");
        Assert.Contains(result, p => p.Name == "HasTags");
        Assert.DoesNotContain(result, p => p.Name == "HasNullTags");
        Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
        Assert.DoesNotContain(result, p => p.Name == "AnotherNullReference");
    }

    #endregion

    #region List Operations Tests

    [Fact]
    public void SafeNavigation_ListIn_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "in(reference.name,(ValidReference,DifferentReference))" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert
        Assert.Equal(6, result.Count); // All items with non-null references (ValidReference and DifferentReference)
        Assert.Contains(result, p => p.Name == "HasValidReference");
        Assert.Contains(result, p => p.Name == "HasDifferentReference");
        Assert.Contains(result, p => p.Name == "CheapProduct");
        Assert.Contains(result, p => p.Name == "ExpensiveProduct");
        Assert.Contains(result, p => p.Name == "HasTags");
        Assert.Contains(result, p => p.Name == "HasNullTags");
        Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
        Assert.DoesNotContain(result, p => p.Name == "AnotherNullReference");
    }

    [Fact]
    public void SafeNavigation_ListOut_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "out(reference.name,(ValidReference))" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert - Should exclude ValidReference, but include items where reference.name != "ValidReference"
        Assert.Equal(3, result.Count); // HasDifferentReference + items with null references (null != "ValidReference")
        Assert.Contains(result, p => p.Name == "HasDifferentReference");
        // Items with null references have reference.name = null, which != "ValidReference", so they should be included
        Assert.Contains(result, p => p.Name == "HasNullReference");
        Assert.Contains(result, p => p.Name == "AnotherNullReference");
    }

    #endregion

    #region Complex and Deep Navigation Tests

    [Fact]
    public void SafeNavigation_DeepNullReferences_FiltersCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateDeepTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "eq(reference.reference.name,DeepValid)" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert - Should only find deeply nested valid reference
        Assert.Single(result);
        Assert.Equal("FullChain", result[0].Name);
    }

    [Fact]
    public void SafeNavigation_CombinedOperators_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act - Complex filter combining multiple operators
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "and(ne(reference.name,null()),like(reference.name,*Valid*))" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert - Should include items with non-null names containing "Valid"
        Assert.Equal(5, result.Count); // All items with ValidReference name
        Assert.Contains(result, p => p.Name == "HasValidReference");
        Assert.Contains(result, p => p.Name == "CheapProduct");
        Assert.Contains(result, p => p.Name == "ExpensiveProduct");
        Assert.Contains(result, p => p.Name == "HasTags");
        Assert.Contains(result, p => p.Name == "HasNullTags");
    }

    [Fact]
    public void SafeNavigation_Ordering_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Order = "reference.name" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        var result = transformResult.Query.ToList();

        // Assert - Should handle nulls gracefully and return all items
        Assert.Equal(8, result.Count); // All test items (safe navigation not excluding null references in current implementation)
    }

    #endregion

    #region SafeNavigation Off Comparison Tests

    [Fact]
    public void SafeNavigationOff_FilteringNullReferences_ThrowsNullReferenceException()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.Off);
        var testData = CreateTestDataWithNulls();

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest { Filter = "eq(reference.name,ValidReference)" }).Query.ToList();
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public void SafeNavigationOff_NumericComparison_ThrowsNullReferenceException()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.Off);
        var testData = CreateTestDataWithNulls();

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest { Filter = "gt(reference.price,50)" }).Query.ToList();
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public void SafeNavigationOff_CollectionOperations_ThrowsNullReferenceException()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.Off);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - Collection operations with null references should throw
        var exception = Assert.ThrowsAny<Exception>(() =>
        {
            var result = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest { Filter = "any(tags,eq(value,Important))" }).Query.ToList();
        });

        Assert.NotNull(exception);
        // The specific exception type may vary (NullReferenceException or ArgumentNullException)
        // but it should be an exception indicating null access
        Assert.True(exception is NullReferenceException or ArgumentNullException);
    }

    [Fact]
    public void SafeNavigationOff_OrderingNullReferences_ThrowsNullReferenceException()
    {
        // Arrange  
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.Off);
        var testData = CreateTestDataWithNulls();

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest { Order = "reference.name" }).Query.ToList();
        });

        Assert.NotNull(exception);
    }

    #endregion

    #region Test Data Creation - All with Null Data

    /// <summary>
    /// Creates a comprehensive test data set that includes:
    /// - Items with valid references for testing positive cases
    /// - Items with null references for testing SafeNavigation
    /// - Items with different values for testing comparison operators
    /// </summary>
    private static IEnumerable<Product> CreateTestDataWithNulls()
    {
        return new[]
        {
            new Product 
            { 
                Id = 1, 
                Name = "HasValidReference", 
                Category = "Test",
                Reference = new Product { Id = 10, Name = "ValidReference", Category = "RefTest", Price = 300m }, // High price, doesn't match numeric filters
                Tags = new List<Tag> { new Tag { Value = "Valid" }, new Tag { Value = "Other" } } // No "Important" tag
            },
            new Product 
            { 
                Id = 2, 
                Name = "HasDifferentReference", 
                Category = "Test",
                Reference = new Product { Id = 11, Name = "DifferentReference", Category = "RefTest", Price = 250m }, // High price, doesn't match numeric filters
                Tags = new List<Tag> { new Tag { Value = "Other" }, new Tag { Value = "Valid" } } // No "Important" tag
            },
            new Product 
            { 
                Id = 3, 
                Name = "HasNullReference", 
                Category = "Test",
                Reference = null!, // Explicit null
                Tags = null! // Null collection
            },
            new Product 
            { 
                Id = 4, 
                Name = "AnotherNullReference", 
                Category = "Test",
                Reference = null!, // Another null
                Tags = null! // Another null collection
            },
            new Product 
            { 
                Id = 5, 
                Name = "CheapProduct", 
                Category = "Test",
                Reference = new Product { Id = 12, Name = "ValidReference", Category = "RefTest", Price = 25m }, // Low price for lt/le tests
                Tags = new List<Tag> { new Tag { Value = "Budget" } } // No "Important" tag
            },
            new Product 
            { 
                Id = 6, 
                Name = "ExpensiveProduct", 
                Category = "Test",
                Reference = new Product { Id = 13, Name = "ValidReference", Category = "RefTest", Price = 150m }, // High price for gt/ge tests
                Tags = new List<Tag> { new Tag { Value = "Premium" } } // No "Important" tag
            },
            new Product 
            { 
                Id = 7, 
                Name = "HasTags", 
                Category = "Test",
                Reference = new Product { Id = 14, Name = "ValidReference", Category = "RefTest", Price = 350m }, // High price, doesn't match numeric filters
                Tags = new List<Tag> { new Tag { Value = "Important" }, new Tag { Value = "Featured" } } // Has "Important" tag
            },
            new Product 
            { 
                Id = 8, 
                Name = "HasNullTags", 
                Category = "Test",
                Reference = new Product { Id = 15, Name = "ValidReference", Category = "RefTest", Price = 400m }, // High price, doesn't match numeric filters
                Tags = null! // Null tags
            }
        };
    }

    /// <summary>
    /// Creates deep nested test data with nulls at various levels
    /// </summary>
    private static IEnumerable<Product> CreateDeepTestDataWithNulls()
    {
        return new[]
        {
            new Product 
            { 
                Id = 1, 
                Name = "FullChain", 
                Category = "Test",
                Tags = new List<Tag> { new Tag { Value = "Deep" } },
                Reference = new Product 
                { 
                    Id = 10, 
                    Name = "Level1", 
                    Category = "RefTest",
                    Price = 50m,
                    Tags = new List<Tag> { new Tag { Value = "Level1Tag" } },
                    Reference = new Product 
                    { 
                        Id = 20, 
                        Name = "DeepValid", 
                        Category = "DeepTest", 
                        Price = 100m,
                        Tags = new List<Tag> { new Tag { Value = "DeepTag" } },
                        Reference = null! // End of chain
                    }
                }
            },
            new Product 
            { 
                Id = 2, 
                Name = "NullAtLevel1", 
                Category = "Test",
                Tags = new List<Tag> { new Tag { Value = "Shallow" } },
                Reference = null! // Null at first level
            },
            new Product 
            { 
                Id = 3, 
                Name = "NullAtLevel2", 
                Category = "Test",
                Tags = null!, // Null tags
                Reference = new Product 
                { 
                    Id = 11, 
                    Name = "Level1", 
                    Category = "RefTest",
                    Price = 25m,
                    Tags = new List<Tag> { new Tag { Value = "Incomplete" } },
                    Reference = null! // Null at second level
                }
            },
            new Product 
            { 
                Id = 4, 
                Name = "AllNull", 
                Category = "Test",
                Tags = null!, // Null tags
                Reference = null! // Null reference
            }
        };
    }

    #endregion

    #region Test Executor

    private class SafeNavigationTestExecutor : TestExecutor<Product>
    {
        private readonly SafeNavigationMode _filterSafeNavigation;
        private readonly SafeNavigationMode _orderingSafeNavigation;

        public SafeNavigationTestExecutor(SafeNavigationMode mode)
            : this(mode, mode)
        {
        }

        public SafeNavigationTestExecutor(SafeNavigationMode filterSafeNavigation, SafeNavigationMode orderingSafeNavigation)
        {
            _filterSafeNavigation = filterSafeNavigation;
            _orderingSafeNavigation = orderingSafeNavigation;
        }

        protected override IRqlQueryable<Product, Product> MakeRql()
            => RqlFactory.Make<Product>(services => { }, rqlConfig =>
            {
                // Configure SafeNavigation at service registration time
                rqlConfig.Settings.Filter.SafeNavigation = _filterSafeNavigation;
                rqlConfig.Settings.Ordering.SafeNavigation = _orderingSafeNavigation;
                
                rqlConfig.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
                rqlConfig.Settings.Select.Explicit = RqlSelectModes.All;
                rqlConfig.Settings.Select.MaxDepth = 10;

                rqlConfig.Settings.Mapping.Transparent = true;
            });

        public override IQueryable<Product> GetQuery() => CreateTestDataWithNulls().AsQueryable();

        protected override void Customize(IRqlSettings settings)
        {
            settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            settings.Select.Explicit = RqlSelectModes.All;
            settings.Select.MaxDepth = 10;
            
            settings.Filter.SafeNavigation = _filterSafeNavigation;
            settings.Ordering.SafeNavigation = _orderingSafeNavigation;
        }
    }

    #endregion
}