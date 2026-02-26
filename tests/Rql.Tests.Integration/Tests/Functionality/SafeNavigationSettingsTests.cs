using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Settings;
using Rql.Tests.Integration.Core;
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
        Assert.Equal(NavigationStrategy.Default, settings.Filter.Navigation);
        Assert.Equal(NavigationStrategy.Default, settings.Ordering.Navigation);
    }

    [Fact]
    public void SafeNavigationSettings_CanBeSetIndependently()
    {
        // Arrange
        var settings = new RqlSettings();

        // Act
        settings.Filter.Navigation = NavigationStrategy.Safe;
        settings.Ordering.Navigation = NavigationStrategy.Default;
        settings.Mapping.Navigation = NavigationStrategy.Default;

        // Assert
        Assert.Equal(NavigationStrategy.Safe, settings.Filter.Navigation);
        Assert.Equal(NavigationStrategy.Default, settings.Ordering.Navigation);
        Assert.Equal(NavigationStrategy.Default, settings.Mapping.Navigation);
    }

    [Fact]
    public void SafeNavigationIndependent_FilterOnOrderingOff_ThrowsOnOrdering()
    {
        // Arrange - Filter safe, ordering not safe
        var rql = CreateRql(
            filterNavigation: NavigationStrategy.Safe,
            orderingNavigation: NavigationStrategy.Default);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - Filtering should work, but ordering should fail
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var query = rql.Transform(testData.AsQueryable(),
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
        var rql = CreateRql(
            filterNavigation: NavigationStrategy.Default,
            orderingNavigation: NavigationStrategy.Safe);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - Filtering should fail, ordering doesn't get reached
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = rql.Transform(testData.AsQueryable(),
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
        var rql = CreateRql(
            filterNavigation: NavigationStrategy.Safe,
            orderingNavigation: NavigationStrategy.Safe);
        var testData = CreateTestDataWithNulls();

        // Act - Both filtering and ordering should work
        var transformResult = rql.Transform(testData.AsQueryable(),
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
        var rql = CreateRql(
            filterNavigation: NavigationStrategy.Default,
            orderingNavigation: NavigationStrategy.Default);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - Should fail on filtering (first operation)
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = rql.Transform(testData.AsQueryable(),
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
        var rqlDefault = CreateRql(NavigationStrategy.Default);
        var testData = CreateTestDataWithNulls();

        // Act & Assert - First should fail
        var exception = Assert.Throws<NullReferenceException>(() =>
        {
            var result = rqlDefault.Transform(testData.AsQueryable(),
                new RqlRequest { Filter = "eq(reference.name,ValidReference)" }).Query.ToList();
        });

        Assert.NotNull(exception);

        // Now change settings to safe mode
        var rqlSafe = CreateRql(NavigationStrategy.Safe);

        // Should work now
        var transformResult = rqlSafe.Transform(testData.AsQueryable(),
            new RqlRequest { Filter = "eq(reference.name,ValidReference)" });

        Assert.True(transformResult.IsSuccess);
        var result = transformResult.Query.ToList();
        Assert.Single(result);
        Assert.Equal("HasValidReference", result[0].Name);
    }

    #region Helper Methods

    private static IRqlQueryable<Product, Product> CreateRql(NavigationStrategy filterNavigation, NavigationStrategy orderingNavigation)
        => RqlFactory.Make<Product>(services => { }, rqlConfig =>
        {
            rqlConfig.Settings.Filter.Navigation = filterNavigation;
            rqlConfig.Settings.Ordering.Navigation = orderingNavigation;
            rqlConfig.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rqlConfig.Settings.Select.Explicit = RqlSelectModes.All;
            rqlConfig.Settings.Select.MaxDepth = 10;
            rqlConfig.Settings.Mapping.Transparent = true;
        });

    private static IRqlQueryable<Product, Product> CreateRql(NavigationStrategy mode)
        => CreateRql(mode, mode);

    #endregion

    #region Test Data Creation

    private static IEnumerable<Product> CreateTestDataWithNulls()
    {
        return
        [
            new()
            {
                Id = 1,
                Name = "HasValidReference",
                Category = "Test",
                Reference = new() { Id = 10, Name = "ValidReference", Category = "RefTest" },
                Tags = [new() { Value = "Test" }]
            },
            new()
            {
                Id = 2,
                Name = "HasNullReference",
                Category = "Test",
                Reference = null!, // Explicit null
                Tags = null! // Null collection
            },
            new()
            {
                Id = 3,
                Name = "AnotherNullReference",
                Category = "Test",
                Reference = null!, // Another null
                Tags = null! // Another null collection
            }
        ];
    }

    #endregion
}