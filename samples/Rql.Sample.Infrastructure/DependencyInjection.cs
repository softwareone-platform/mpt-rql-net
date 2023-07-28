using Microsoft.Extensions.DependencyInjection;
using Rql.Sample.Application.Common.Interfaces.Persistence.AdventureWorks;
using Rql.Sample.Application.Common.Interfaces.Persistence.InMemory;
using Rql.Sample.Infrastructure.Persistence.Ef;
using Rql.Sample.Infrastructure.Persistence.Ef.Repositories;
using Rql.Sample.Infrastructure.Persistence.InMemory.Repositories;
using System.Net;

namespace Rql.Sample.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddPersistance();
        return services;
    }

    public static IServiceCollection AddPersistance(
        this IServiceCollection services)
    {
        services.AddSingleton<ISampleRepository, SampleRepository>();
        services.AddScoped<IProductsRepository, ProductsRepository>();
        services.AddScoped<IAddressesRepository, AddressesRepoository>();
        services.AddDbContext<AvDbContext>();

        return services;
    }
}