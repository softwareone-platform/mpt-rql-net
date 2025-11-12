using System.Linq.Expressions;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class SafeNavigationNullDataTests
{
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
    public void SafeNavigationOn_FilteringNullReferences_FiltersCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(), 
            new RqlRequest { Filter = "eq(reference.name,ValidReference)" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        
        var result = transformResult.Query.ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("HasValidReference", result[0].Name);
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

    [Fact]
    public void SafeNavigationOn_OrderingNullReferences_HandlesGracefully()
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
        Assert.Equal(3, result.Count);
        // Items with null references should typically sort to end or beginning
        Assert.Contains(result, p => p.Name == "HasValidReference");
        Assert.Contains(result, p => p.Name == "HasNullReference");
        Assert.Contains(result, p => p.Name == "AnotherNullReference");
    }

    [Fact]
    public void SafeNavigationOn_DeepNullReferences_FiltersCorrectly()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        var testData = CreateDeepTestDataWithNulls();

        // Act
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "eq(reference.reference.name,DeepValid)" });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        
        var result = transformResult.Query.ToList();

        // Assert - Should find only the item with complete chain
        Assert.Single(result);
        Assert.Equal("FullChain", result[0].Name);
    }

    [Fact]
    public void SafeNavigationOff_DeepNullReferences_ThrowsNullReferenceException()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.Off);
        var testData = CreateDeepTestDataWithNulls();

        // Act & Assert
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest { Filter = "eq(reference.reference.name,DeepValid)" }).Query.ToList();
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public void SafeNavigationIndependent_FilterOnOrderingOff_ThrowsOnOrdering()
    {
        // Arrange - Filter safe, ordering not safe
        var testExecutor = new SafeNavigationTestExecutor(
            filterSafeNavigation: SafeNavigationMode.On,
            orderingSafeNavigation: SafeNavigationMode.Off);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - Filtering should work, but ordering should fail
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var query = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest
                {
                    Order = "reference.name"                       // Should fail
                }).Query;
            var result = query.ToList();
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public void SafeNavigationIndependent_OrderOnFilteringOff_ThrowsOnFiltering()
    {
        // Arrange - Ordering safe, filtering not safe
        var testExecutor = new SafeNavigationTestExecutor(
            filterSafeNavigation: SafeNavigationMode.Off,
            orderingSafeNavigation: SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - Filtering should fail, ordering doesn't get reached
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest 
                { 
                    Filter = "eq(reference.name,ValidReference)",  // Should fail
                    Order = "reference.name"                       // Doesn't get reached
                }).Query.ToList();
        });

        Assert.NotNull(exception);
    }

    private static IEnumerable<Product> CreateTestDataWithNulls()
    {
        return new[]
        {
            new Product 
            { 
                Id = 1, 
                Name = "HasValidReference", 
                Category = "Test",
                Reference = new Product { Id = 10, Name = "ValidReference", Category = "RefTest" }
            },
            new Product 
            { 
                Id = 2, 
                Name = "HasNullReference", 
                Category = "Test",
                Reference = null! // Explicit null
            },
            new Product 
            { 
                Id = 3, 
                Name = "AnotherNullReference", 
                Category = "Test",
                Reference = null! // Another null
            }
        };
    }

    private static IEnumerable<Product> CreateDeepTestDataWithNulls()
    {
        return new[]
        {
            new Product 
            { 
                Id = 1, 
                Name = "FullChain", 
                Category = "Test",
                Reference = new Product 
                { 
                    Id = 10, 
                    Name = "Level1", 
                    Category = "RefTest",
                    Reference = new Product { Id = 20, Name = "DeepValid", Category = "DeepTest" }
                }
            },
            new Product 
            { 
                Id = 2, 
                Name = "NullAtLevel1", 
                Category = "Test",
                Reference = null! // Null at first level
            },
            new Product 
            { 
                Id = 3, 
                Name = "NullAtLevel2", 
                Category = "Test",
                Reference = new Product 
                { 
                    Id = 11, 
                    Name = "Level1", 
                    Category = "RefTest",
                    Reference = null! // Null at second level
                }
            }
        };
    }

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

        public override IQueryable<Product> GetQuery() => ProductRepository.Query();

        protected override void Customize(IRqlSettings settings)
        {
            // This method is called during Transform, but settings might already be baked in
            // We'll still set them here for completeness, but the real configuration should be in MakeRql
            settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            settings.Select.Explicit = RqlSelectModes.All;
            settings.Select.MaxDepth = 10;
            
            settings.Filter.SafeNavigation = _filterSafeNavigation;
            settings.Ordering.SafeNavigation = _orderingSafeNavigation;
        }
    }
}