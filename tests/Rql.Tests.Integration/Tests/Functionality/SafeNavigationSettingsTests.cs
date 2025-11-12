using System.Linq.Expressions;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Settings;
using Rql.Tests.Integration.Core;
using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Tests for SafeNavigation configuration and settings behavior.
/// These tests verify that SafeNavigation can be configured independently
/// for filtering and ordering operations, and default values are correct.
/// </summary>
public class SafeNavigationSettingsTests
{
    [Fact]
    public void SafeNavigationSettings_DefaultValues_AreOff()
    {
        // Arrange & Act
        var settings = new RqlSettings();
        
        // Assert
        Assert.Equal(SafeNavigationMode.Off, settings.Filter.SafeNavigation);
        Assert.Equal(SafeNavigationMode.Off, settings.Ordering.SafeNavigation);
    }

    [Fact]
    public void SafeNavigationSettings_CanBeSetIndependently()
    {
        // Arrange
        var settings = new RqlSettings();
        
        // Act
        settings.Filter.SafeNavigation = SafeNavigationMode.On;
        settings.Ordering.SafeNavigation = SafeNavigationMode.Off;
        
        // Assert
        Assert.Equal(SafeNavigationMode.On, settings.Filter.SafeNavigation);
        Assert.Equal(SafeNavigationMode.Off, settings.Ordering.SafeNavigation);
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

    [Fact]
    public void SafeNavigationBoth_FilterAndOrderingOn_HandlesNullsCorrectly()
    {
        // Arrange - Both filter and ordering safe
        var testExecutor = new SafeNavigationTestExecutor(
            filterSafeNavigation: SafeNavigationMode.On,
            orderingSafeNavigation: SafeNavigationMode.On);
        var testData = CreateTestDataWithNulls();

        // Act - Both filtering and ordering should work
        var transformResult = testExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest 
            { 
                Filter = "eq(reference.name,ValidReference)",
                Order = "reference.name"
            });

        Assert.True(transformResult.IsSuccess, $"Transform failed: {string.Join(", ", transformResult.Errors?.Select(e => e.Message) ?? [])}");
        
        var result = transformResult.Query.ToList();

        // Assert - Should find the valid reference and handle ordering gracefully
        Assert.Single(result);
        Assert.Equal("HasValidReference", result[0].Name);
    }

    [Fact]
    public void SafeNavigationBoth_FilterAndOrderingOff_ThrowsOnFiltering()
    {
        // Arrange - Both filter and ordering unsafe
        var testExecutor = new SafeNavigationTestExecutor(
            filterSafeNavigation: SafeNavigationMode.Off,
            orderingSafeNavigation: SafeNavigationMode.Off);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - Should fail on filtering (first operation)
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

    [Fact]
    public void SafeNavigationSettings_CanBeChangedAtRuntime()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.Off);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - First should fail
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = testExecutor.Rql.Transform(testData.AsQueryable(),
                new RqlRequest { Filter = "eq(reference.name,ValidReference)" }).Query.ToList();
        });

        Assert.NotNull(exception);

        // Now change settings to safe mode
        var safeExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        
        // Should work now
        var transformResult = safeExecutor.Rql.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "eq(reference.name,ValidReference)" });

        Assert.True(transformResult.IsSuccess);
        var result = transformResult.Query.ToList();
        Assert.Single(result);
        Assert.Equal("HasValidReference", result[0].Name);
    }

    #region Test Data Creation

    private static IEnumerable<Product> CreateTestDataWithNulls()
    {
        return new[]
        {
            new Product 
            { 
                Id = 1, 
                Name = "HasValidReference", 
                Category = "Test",
                Reference = new Product { Id = 10, Name = "ValidReference", Category = "RefTest" },
                Tags = new List<Tag> { new Tag { Value = "Test" } }
            },
            new Product 
            { 
                Id = 2, 
                Name = "HasNullReference", 
                Category = "Test",
                Reference = null!, // Explicit null
                Tags = null! // Null collection
            },
            new Product 
            { 
                Id = 3, 
                Name = "AnotherNullReference", 
                Category = "Test",
                Reference = null!, // Another null
                Tags = null! // Another null collection
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

        public override IQueryable<Product> GetQuery() => CreateSafeNavigationTestData().AsQueryable();

        private static IEnumerable<Product> CreateSafeNavigationTestData()
        {
            return new[]
            {
                new Product 
                { 
                    Id = 1, 
                    Name = "HasValidReference", 
                    Category = "Test",
                    Reference = new Product { Id = 10, Name = "ValidReference", Category = "RefTest", Price = 75m },
                    Tags = new List<Tag> { new Tag { Value = "Important" }, new Tag { Value = "Valid" } },
                    Orders = new List<ProductOrder>(),
                    OrdersIds = new List<int>()
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "HasNullReference", 
                    Category = "Test",
                    Reference = null!,
                    Tags = null!,
                    Orders = new List<ProductOrder>(),
                    OrdersIds = new List<int>()
                }
            };
        }

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

    #endregion
}