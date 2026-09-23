using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Services.Ordering.Functions;
using Rql.Tests.Unit.Services.Models;
using Xunit;

namespace Rql.Tests.Unit;

/// <summary>
/// The container now mixes lifetimes (singleton ordering functions and registry consumed by scoped services),
/// so the registration graph is validated the strict way a host would validate it in Development.
/// </summary>
public class RqlExtensionsTests
{
    private static ServiceProvider BuildValidated()
        => new ServiceCollection()
            .AddRql()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    [Fact]
    public void AddRql_RegistrationGraph_PassesScopeAndBuildValidation()
    {
        using var provider = BuildValidated();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IRqlQueryable<Product>>().Should().NotBeNull();
    }

    [Fact]
    public void OrderingFunctions_AreSharedAcrossScopes()
    {
        using var provider = BuildValidated();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var registry1 = first.ServiceProvider.GetRequiredService<OrderingFunctionRegistry>();
        var registry2 = second.ServiceProvider.GetRequiredService<OrderingFunctionRegistry>();

        registry1.Should().BeSameAs(registry2);
        registry1.TryGet("first", out var function).Should().BeTrue();
        function.Should().BeOfType<FirstOrderingFunction>();
    }
}
