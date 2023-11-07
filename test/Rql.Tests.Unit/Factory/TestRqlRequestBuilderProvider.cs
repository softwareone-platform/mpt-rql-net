using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Request;
using SoftwareOne.Rql.Linq.Client.Core;
using SoftwareOne.Rql.Linq.Client.Generator;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace Rql.Tests.Unit.Factory;

internal class TestRqlRequestBuilderProvider : IRqlRequestBuilderProvider
{
    public IRqlRequestBuilder<T> GetBuilder<T>() where T : class
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPropertyNameProvider, PropertyNameProvider>();
        services.AddScoped<IPropertyVisitor, PropertyVisitor>();
        services.AddSingleton<IOrderGenerator, OrderGenerator>();
        services.AddSingleton<IFilterGenerator, FilterGenerator>();
        services.AddSingleton<ISelectGenerator, SelectGenerator>();
        services.AddSingleton<IRqlRequestGenerator, RqlRequestGenerator>();
        services.AddSingleton<IRqlRequestBuilderProvider, RqlRequestBuilderProvider>();


        services.AddScoped(typeof(IRqlRequestBuilder<>), typeof(RqlRequestBuilder<>));
        services.AddTransient(typeof(IRqlRequestBuilderContext<>), typeof(RqlRequestBuilderContext<>));

        services.AddSingleton<IRqlRequestBuilderProvider, RqlRequestBuilderProvider>();

        var serviceProvider = services.BuildServiceProvider();

        return new RqlRequestBuilder<T>(serviceProvider);
    }
}