using Microsoft.Extensions.DependencyInjection;
using Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;
using SoftwareOne.Rql.Abstractions.Exception;
using Xunit;
using Xunit.Sdk;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy;

public class BasicActionStrategyTests
{
    private readonly ActionStrategyTestExecutor _testExecutor;

    public BasicActionStrategyTests()
    {
        _testExecutor = new ActionStrategyTestExecutor(services =>
        {
            services.AddSingleton<AllowAllActionStrategy>();
            services.AddSingleton<AllowNothingActionStrategy>();
            services.AddSingleton<AllowOnlyFilterActionStrategy>();
            services.AddSingleton<AllowOnlyOrderActionStrategy>();
            services.AddSingleton<AllowOnlySelectActionStrategy>();
        });
    }

    [Fact]
    public void Shape_NoChange() => _testExecutor.ShapeMatch(t =>
    {
        t.Nothing = null!;
        t.FilterOnly = null!;
        t.OrderOnly = null!;
    }, string.Empty);

    [Fact]
    public void Shape_FilterOnlyIncluded_NoChange() => _testExecutor.ShapeMatch(t =>
    {
        t.Nothing = null!;
        t.FilterOnly = null!;
        t.OrderOnly = null!;
    }, "FilterOnly");

    [Fact]
    public void Invalid_Strategy_Should_Throw()
    {
        Assert.ThrowsAny<RqlInvalidActionStrategyException>(() =>
        {
            var executor = new ActionStrategyInvalidTestExecutor();
            executor.MustFailWithError();
        });
    }

    [Fact]
    public void Unregistrered_Strategy_Should_Throw()
    {
        Assert.ThrowsAny<RqlInvalidActionStrategyException>(() =>
        {
            var executor = new ActionStrategyTestExecutor(services => { });
            executor.MustFailWithError(filter: "All.Foo=abc");
        });
    }

    [Theory]
    [InlineData("Nothing")]
    [InlineData("FilterOnly")]
    [InlineData("SelectOnly")]
    public void Order_Forbidden_Should_Fail(string orderingExpression) =>
        _testExecutor.MustFailWithError(order: orderingExpression, errorDescription: "Ordering is not permitted.");

    [Theory]
    [InlineData("Nothing.Foo=abc")]
    [InlineData("OrderOnly.Foo=abc")]
    [InlineData("SelectOnly.Foo=abc")]
    public void Filter_Forbidden_Should_Fail(string filterExpression) =>
        _testExecutor.MustFailWithError(filter: filterExpression, errorDescription: "Filtering is not permitted.");
}