using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Exception;
using Rql.Tests.Integration.Core;
using Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy;

public class BasicActionStrategyTests
{
    private readonly IRqlQueryable<ActionStrategyTestItem, ActionStrategyTestItem> _rql;

    public BasicActionStrategyTests()
    {
        _rql = RqlFactory.Make<ActionStrategyTestItem>(services =>
        {
            services.AddScoped<AllowAllActionStrategy>();
            services.AddScoped<AllowNothingActionStrategy>();
            services.AddScoped<AllowOnlyFilterActionStrategy>();
            services.AddScoped<AllowOnlyOrderActionStrategy>();
            services.AddScoped<AllowOnlySelectActionStrategy>();
        }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
        });
    }

    [Fact]
    public void Shape_NoChange()
    {
        // Arrange
        var testData = ActionStrategyTestItemRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Select = string.Empty });

        // Assert
        Assert.True(result.IsSuccess);
        var item = result.Query.First();
        Assert.Null(item.Nothing);
        Assert.Null(item.FilterOnly);
        Assert.Null(item.OrderOnly);
    }

    [Fact]
    public void Shape_FilterOnlyIncluded_NoChange()
    {
        // Arrange
        var testData = ActionStrategyTestItemRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Select = "FilterOnly" });

        // Assert
        Assert.True(result.IsSuccess);
        var item = result.Query.First();
        Assert.Null(item.Nothing);
        Assert.Null(item.FilterOnly);
        Assert.Null(item.OrderOnly);
    }

    [Fact]
    public void Invalid_Strategy_Should_Throw()
    {
        Assert.ThrowsAny<RqlInvalidActionStrategyException>(() =>
        {
            var rqlInvalid = RqlFactory.Make<ActionStrategyInvalidTestItem>(services => { });
            var testData = new[] { new ActionStrategyInvalidTestItem { Id = 1 } }.AsQueryable();
            var result = rqlInvalid.Transform(testData, new RqlRequest { });
        });
    }

    [Fact]
    public void Unregistrered_Strategy_Should_Throw()
    {
        Assert.ThrowsAny<RqlInvalidActionStrategyException>(() =>
        {
            var rqlEmpty = RqlFactory.Make<ActionStrategyTestItem>(services => { });
            var testData = ActionStrategyTestItemRepository.Query();
            var result = rqlEmpty.Transform(testData, new RqlRequest { Filter = "All.Foo=abc" });
        });
    }

    [Theory]
    [InlineData("Nothing")]
    [InlineData("FilterOnly")]
    [InlineData("SelectOnly")]
    public void Order_Forbidden_Should_Fail(string orderingExpression)
    {
        // Arrange
        var testData = ActionStrategyTestItemRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Order = orderingExpression });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Ordering is not permitted.", result.Errors.First().Message);
    }

    [Theory]
    [InlineData("Nothing.Foo=abc")]
    [InlineData("OrderOnly.Foo=abc")]
    [InlineData("SelectOnly.Foo=abc")]
    public void Filter_Forbidden_Should_Fail(string filterExpression)
    {
        // Arrange
        var testData = ActionStrategyTestItemRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Filter = filterExpression });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Filtering is not permitted.", result.Errors.First().Message);
    }
}