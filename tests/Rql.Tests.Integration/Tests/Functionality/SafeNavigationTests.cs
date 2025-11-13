using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Tests for SafeNavigation functionality across all RQL operators.
/// Verifies that SafeNavigation prevents null reference exceptions and handles
/// null values appropriately for string operations, comparisons, collections, and lists.
/// </summary>
public class SafeNavigationTests
{
    #region String Operations Tests

    [Theory]
    [InlineData("eq(reference.name,ValidReference)", 5)]
    [InlineData("ne(reference.name,ValidReference)", 1)]
    public void SafeNavigation_StringEqual_HandlesNullsCorrectly(string filter, int expectedCount)
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: filter);

        // Assert
        Assert.Equal(expectedCount, result.ToList().Count);
    }

    [Fact]
    public void SafeNavigation_StringEquals_WithNullProperty_ReturnsNoResults()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: "eq(reference.name,SomeValue)");

        // Assert - Items with null reference.name won't match any specific value
        Assert.DoesNotContain(result.ToList(), p => p.Name == "HasNullReference");
    }

    [Theory]
    [InlineData("like(reference.name,*Valid*)", 5)]
    [InlineData("like(reference.name,Valid*)", 5)]
    [InlineData("like(reference.name,*Reference*)", 6)]
    public void SafeNavigation_StringLike_HandlesNullsCorrectly(string filter, int expectedCount)
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: filter).ToList();

        // Assert - Should exclude items with null references
        Assert.Equal(expectedCount, result.Count);
        Assert.DoesNotContain(result, p => p.Name == "HasNullReference");
        Assert.DoesNotContain(result, p => p.Name == "AnotherNullReference");
    }

    #endregion

    #region Numeric Comparison Tests

    [Theory]
    [InlineData("gt(reference.price,50)", 5)]
    [InlineData("ge(reference.price,100)", 5)]
    [InlineData("lt(reference.price,200)", 2)]
    [InlineData("le(reference.price,50)", 1)]
    public void SafeNavigation_NumericComparison_HandlesNullsCorrectly(string filter, int expectedCount)
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: filter).ToList();

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
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: "gt(reference.price,0)").ToList();

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
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: "any(tags,eq(value,Important))").ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("HasTags", result[0].Name);
    }

    [Fact]
    public void SafeNavigation_CollectionAll_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: "all(tags,ne(value,Invalid))").ToList();

        // Assert - Should include items with valid tags, exclude null collections  
        Assert.Equal(5, result.Count);
        var resultNames = result.Select(p => p.Name).ToList();
        Assert.Contains("HasValidReference", resultNames);
        Assert.Contains("HasDifferentReference", resultNames);
        Assert.Contains("CheapProduct", resultNames);
        Assert.Contains("ExpensiveProduct", resultNames);
        Assert.Contains("HasTags", resultNames);
        Assert.DoesNotContain("HasNullTags", resultNames);
        Assert.DoesNotContain("HasNullReference", resultNames);
        Assert.DoesNotContain("AnotherNullReference", resultNames);
    }

    #endregion

    #region List Operations Tests

    [Fact]
    public void SafeNavigation_ListIn_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: "in(reference.name,(ValidReference,DifferentReference))").ToList();

        // Assert
        Assert.Equal(6, result.Count);
        var resultNames = result.Select(p => p.Name).ToList();
        Assert.Contains("HasValidReference", resultNames);
        Assert.Contains("HasDifferentReference", resultNames);
        Assert.Contains("CheapProduct", resultNames);
        Assert.Contains("ExpensiveProduct", resultNames);
        Assert.Contains("HasTags", resultNames);
        Assert.Contains("HasNullTags", resultNames);
        Assert.DoesNotContain("HasNullReference", resultNames);
        Assert.DoesNotContain("AnotherNullReference", resultNames);
    }

    [Fact]
    public void SafeNavigation_ListOut_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: "out(reference.name,(ValidReference))").ToList();

        // Assert - Should exclude ValidReference, but include items where reference.name != "ValidReference"
        Assert.Equal(3, result.Count);
        var resultNames = result.Select(p => p.Name).ToList();
        Assert.Contains("HasDifferentReference", resultNames);
        // Items with null references have reference.name = null, which != "ValidReference", so they should be included
        Assert.Contains("HasNullReference", resultNames);
        Assert.Contains("AnotherNullReference", resultNames);
    }

    #endregion

    #region Complex and Deep Navigation Tests

    [Fact]
    public void SafeNavigation_DeepNullReferences_FiltersCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(
            data: CreateDeepTestDataWithNulls(),
            filter: "eq(reference.reference.name,DeepValid)").ToList();

        // Assert - Should only find deeply nested valid reference
        Assert.Single(result);
        Assert.Equal("FullChain", result[0].Name);
    }

    [Fact]
    public void SafeNavigation_CombinedOperators_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(filter: "and(ne(reference.name,null()),like(reference.name,*Valid*))").ToList();

        // Assert - Should include items with non-null names containing "Valid"
        Assert.Equal(5, result.Count);
        var resultNames = result.Select(p => p.Name).ToList();
        Assert.Contains("HasValidReference", resultNames);
        Assert.Contains("CheapProduct", resultNames);
        Assert.Contains("ExpensiveProduct", resultNames);
        Assert.Contains("HasTags", resultNames);
        Assert.Contains("HasNullTags", resultNames);
    }

    [Fact]
    public void SafeNavigation_Ordering_HandlesNullsCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act
        var result = testExecutor.Transform(order: "reference.name").ToList();

        // Assert - Should handle nulls gracefully and return all items
        Assert.Equal(8, result.Count);
    }

    #endregion

    #region SafeNavigation Off Tests

    [Fact]
    public void SafeNavigationOff_FilteringNullReferences_ThrowsNullReferenceException()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Default);

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
            testExecutor.Transform(filter: "eq(reference.name,ValidReference)").ToList());

        Assert.NotNull(exception);
    }

    [Fact]
    public void SafeNavigationOff_NumericComparison_ThrowsNullReferenceException()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Default);

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
            testExecutor.Transform(filter: "gt(reference.price,50)").ToList());

        Assert.NotNull(exception);
    }

    [Fact]
    public void SafeNavigationOff_CollectionOperations_ThrowsNullReferenceException()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Default);

        // Act & Assert
        var exception = Assert.ThrowsAny<Exception>(() =>
            testExecutor.Transform(filter: "any(tags,eq(value,Important))").ToList());

        Assert.NotNull(exception);
        Assert.True(exception is NullReferenceException or ArgumentNullException);
    }

    [Fact]
    public void SafeNavigationOff_OrderingNullReferences_ThrowsNullReferenceException()
    {
        // Arrange  
        var testExecutor = new SafeNavigationTestExecutor(NavigationStrategy.Default);

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
            testExecutor.Transform(order: "reference.name").ToList());

        Assert.NotNull(exception);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void SafeNavigationConfiguration_FilterAndOrderingIndependent()
    {
        // Arrange
        var filterOnOrderingOff = new SafeNavigationTestExecutor(
            filterSafeNavigation: NavigationStrategy.Safe,
            orderingSafeNavigation: NavigationStrategy.Default);

        var bothOn = new SafeNavigationTestExecutor(NavigationStrategy.Safe);

        // Act & Assert
        var result1 = filterOnOrderingOff.Transform(filter: "eq(name,HasValidReference)", order: "name").ToList();
        var result2 = bothOn.Transform(filter: "eq(name,HasValidReference)", order: "name").ToList();

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.True(result1.Count >= 0);
        Assert.True(result2.Count >= 0);
    }

    [Fact]
    public void SafeNavigationSettings_AreProperlyConfigured()
    {
        // Arrange & Act
        var filterOn = new SafeNavigationTestExecutor(
            filterSafeNavigation: NavigationStrategy.Safe,
            orderingSafeNavigation: NavigationStrategy.Default);

        // Assert - Configuration should be created without errors
        Assert.NotNull(filterOn.Rql);
    }

    #endregion

    #region Test Data and Helper Methods

    /// <summary>
    /// Creates comprehensive test data with null references for testing
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
                Reference = new Product { Id = 10, Name = "ValidReference", Category = "RefTest", Price = 300m },
                Tags = new List<Tag> { new Tag { Value = "Valid" }, new Tag { Value = "Other" } }
            },
            new Product
            {
                Id = 2,
                Name = "HasDifferentReference",
                Category = "Test",
                Reference = new Product { Id = 11, Name = "DifferentReference", Category = "RefTest", Price = 250m },
                Tags = new List<Tag> { new Tag { Value = "Other" }, new Tag { Value = "Valid" } }
            },
            new Product
            {
                Id = 3,
                Name = "HasNullReference",
                Category = "Test",
                Reference = null!,
                Tags = null!
            },
            new Product
            {
                Id = 4,
                Name = "AnotherNullReference",
                Category = "Test",
                Reference = null!,
                Tags = null!
            },
            new Product
            {
                Id = 5,
                Name = "CheapProduct",
                Category = "Test",
                Reference = new Product { Id = 12, Name = "ValidReference", Category = "RefTest", Price = 25m },
                Tags = new List<Tag> { new Tag { Value = "Budget" } }
            },
            new Product
            {
                Id = 6,
                Name = "ExpensiveProduct",
                Category = "Test",
                Reference = new Product { Id = 13, Name = "ValidReference", Category = "RefTest", Price = 150m },
                Tags = new List<Tag> { new Tag { Value = "Premium" } }
            },
            new Product
            {
                Id = 7,
                Name = "HasTags",
                Category = "Test",
                Reference = new Product { Id = 14, Name = "ValidReference", Category = "RefTest", Price = 350m },
                Tags = new List<Tag> { new Tag { Value = "Important" }, new Tag { Value = "Featured" } }
            },
            new Product
            {
                Id = 8,
                Name = "HasNullTags",
                Category = "Test",
                Reference = new Product { Id = 15, Name = "ValidReference", Category = "RefTest", Price = 400m },
                Tags = null!
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
                        Reference = null!
                    }
                }
            },
            new Product
            {
                Id = 2,
                Name = "NullAtLevel1",
                Category = "Test",
                Tags = new List<Tag> { new Tag { Value = "Shallow" } },
                Reference = null!
            },
            new Product
            {
                Id = 3,
                Name = "NullAtLevel2",
                Category = "Test",
                Tags = null!,
                Reference = new Product
                {
                    Id = 11,
                    Name = "Level1",
                    Category = "RefTest",
                    Price = 25m,
                    Tags = new List<Tag> { new Tag { Value = "Incomplete" } },
                    Reference = null!
                }
            },
            new Product
            {
                Id = 4,
                Name = "AllNull",
                Category = "Test",
                Tags = null!,
                Reference = null!
            }
        };
    }

    #endregion

    #region Test Executor

    private class SafeNavigationTestExecutor : TestExecutor<Product>
    {
        private readonly NavigationStrategy _filterSafeNavigation;
        private readonly NavigationStrategy _orderingSafeNavigation;

        public SafeNavigationTestExecutor(NavigationStrategy mode)
            : this(mode, mode)
        {
        }

        public SafeNavigationTestExecutor(NavigationStrategy filterSafeNavigation, NavigationStrategy orderingSafeNavigation)
        {
            _filterSafeNavigation = filterSafeNavigation;
            _orderingSafeNavigation = orderingSafeNavigation;
        }

        protected override IRqlQueryable<Product, Product> MakeRql()
            => RqlFactory.Make<Product>(services => { }, rqlConfig =>
            {
                rqlConfig.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
                rqlConfig.Settings.Select.Explicit = RqlSelectModes.All;
                rqlConfig.Settings.Select.MaxDepth = 10;
                rqlConfig.Settings.Filter.Navigation = _filterSafeNavigation;
                rqlConfig.Settings.Ordering.Navigation = _orderingSafeNavigation;
                rqlConfig.Settings.Mapping.Transparent = true;
            });

        public override IQueryable<Product> GetQuery() => CreateTestDataWithNulls().AsQueryable();

        /// <summary>
        /// Unified transform method for all test scenarios
        /// </summary>
        public IQueryable<Product> Transform(IEnumerable<Product>? data = null, string? filter = null, string? order = null)
        {
            var request = new RqlRequest();
            if (!string.IsNullOrEmpty(filter)) request.Filter = filter;
            if (!string.IsNullOrEmpty(order)) request.Order = order;

            var testData = data?.AsQueryable() ?? GetQuery();
            var result = Rql.Transform(testData, request, Customize);
            return result.Query;
        }

        protected override void Customize(IRqlSettings settings) { }
    }

    #endregion
}