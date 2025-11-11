using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Client;
using Mpt.Rql.Client.Builder.Request;
using Mpt.Rql.Client.Core;
using Mpt.Rql.Client.Generator;
using Mpt.Rql.Core.Metadata;

namespace Rql.Tests.Common.Factory;

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