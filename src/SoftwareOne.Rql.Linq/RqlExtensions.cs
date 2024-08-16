using SoftwareOne.Rql;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Abstractions.Unary;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq;
using SoftwareOne.Rql.Linq.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Request;
using SoftwareOne.Rql.Linq.Client.Core;
using SoftwareOne.Rql.Linq.Client.Generator;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Filtering.Builders;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Mapping;
using SoftwareOne.Rql.Linq.Services.Ordering;
using SoftwareOne.Rql.Linq.Services.Projection;
using SoftwareOne.Rql.Parsers.Linear.Domain.Services;
using System.Reflection;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;

public static class RqlExtensions
{
    public static IServiceCollection AddRql(this IServiceCollection services)
        => services.AddRql(null);

    public static IServiceCollection AddRql(this IServiceCollection services, Action<RqlConfiguration>? configure)
    {
        var options = new RqlConfiguration();
        configure?.Invoke(options);

        services.AddScoped<IRqlSettingsAccessor, RqlSettingsAccessor>();
        services.AddScoped(s => s.GetRequiredService<IRqlSettingsAccessor>().Current);
        services.AddSingleton<IRqlGlobalSettings>(options.Settings);

        services.AddSingleton<IRqlParser, RqlParser>();

        services.AddScoped(typeof(IRqlQueryable<>), typeof(RqlQueryable<>));
        services.AddScoped(typeof(IRqlQueryable<,>), typeof(RqlQueryableLinq<,>));

        services.AddScoped(typeof(IMappingService<,>), typeof(MappingService<,>));

        services.AddScoped(typeof(IQueryContext<>), typeof(QueryContext<>));
        services.AddScoped<IExternalServiceAccessor, ExternalServiceAccessor>();

        services.AddScoped(typeof(IFilteringService<>), typeof(FilteringService<>));
        services.AddScoped<IBuilderContext, BuilderContext>();
        services.AddScoped<IExpressionBuilder, ExpressionBuilder>();
        services.AddScoped<IConcreteExpressionBuilder<RqlBinary>, BinaryExpressionBuilder>();
        services.AddScoped<IConcreteExpressionBuilder<RqlCollection>, CollectionExpressionBuilder>();
        services.AddScoped<IConcreteExpressionBuilder<RqlGroup>, GroupExpressionBuilder>();
        services.AddScoped<IConcreteExpressionBuilder<RqlUnary>, UnaryExpressionBuilder>();
        services.AddScoped<IFilteringPathInfoBuilder, FilteringPathInfoBuilder>();
        services.AddScoped(typeof(IFilteringGraphBuilder<>), typeof(FilteringGraphBuilder<>));

        services.AddScoped(typeof(IOrderingService<>), typeof(OrderingService<>));
        services.AddScoped<IOrderingPathInfoBuilder, OrderingPathInfoBuilder>();
        services.AddScoped(typeof(IOrderingGraphBuilder<>), typeof(OrderingGraphBuilder<>));

        services.AddScoped(typeof(IProjectionService<>), typeof(ProjectionService<>));
        services.AddScoped(typeof(IProjectionGraphBuilder<>), typeof(ProjectionGraphBuilder<>));

        services.AddScoped<IActionValidator, ActionValidator>();

        services.AddSingleton<MetadataProvider>();
        services.AddSingleton<IMetadataProvider>(serviceProvider => serviceProvider.GetRequiredService<MetadataProvider>());
        services.AddSingleton<IRqlMetadataProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<MetadataProvider>());
        services.AddSingleton<IEntityMapCache, EntityMapCache>();

        services.AddSingleton<IMetadataFactory, MetadataFactory>();
        services.AddSingleton(typeof(IPropertyNameProvider), options.PropertyMapperType ?? typeof(PropertyNameProvider));

        RegisterClient(services);
        RegisterOperatorExpressions(services, options);

        if (options.ViewMappersAssembly != null)
            ScanForViewMappers(services, options.ViewMappersAssembly);

        services.AddTransient(typeof(IRqlMapperContext<,>), typeof(RqlMapperContext<,>));

        return services;
    }

    private static void RegisterClient(IServiceCollection services)
    {
        services.AddScoped<IPropertyVisitor, PropertyVisitor>();
        services.AddScoped<IOrderGenerator, OrderGenerator>();
        services.AddScoped<IFilterGenerator, FilterGenerator>();
        services.AddScoped<ISelectGenerator, SelectGenerator>();
        services.AddScoped<IRqlRequestGenerator, RqlRequestGenerator>();
        services.AddScoped<IRqlRequestBuilderProvider, RqlRequestBuilderProvider>();

        services.AddScoped(typeof(IRqlRequestBuilder<>), typeof(RqlRequestBuilder<>));
        services.AddTransient(typeof(IRqlRequestBuilderContext<>), typeof(RqlRequestBuilderContext<>));

        services.AddSingleton<IRqlRequestBuilderProvider, RqlRequestBuilderProvider>();
    }

    private static void RegisterOperatorExpressions(IServiceCollection services, RqlConfiguration options)
    {
        var expMapping = new OperatorHandlerMapper();
        var producerType = typeof(SoftwareOne.Rql.Linq.Services.Filtering.Operators.IOperator);
        var types =
            producerType.Assembly.GetTypes().Where(t => t.IsInterface && producerType.IsAssignableFrom(t) && t != producerType).ToList();

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<ExpressionAttribute>();
            if (attribute == null || expMapping.ContainsKey(attribute.Key))
                continue;

            var implementation = options.OperatorOverrides.TryGetValue(type, out var typeValue) ? typeValue : attribute.Implementation;

            services.AddScoped(type, implementation);
            expMapping.Add(attribute.Key, type);
        }

        services.AddSingleton<IOperatorHandlerMapper>(expMapping);
        services.AddScoped<IOperatorHandlerProvider, OperatorHandlerProvider>();
    }

    private static void ScanForViewMappers(IServiceCollection services, Assembly mappingsAssembly)
    {
        var mapTypeRoot = typeof(IRqlMapper);
        var mapTypeGeneric = typeof(IRqlMapper<,>);
        var mapTypes = mappingsAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && mapTypeRoot.IsAssignableFrom(t)).ToList();

        foreach (var mapType in mapTypes)
        {
            var inf = Array.Find(mapType.GetInterfaces(), t => t.IsGenericType && t.GetGenericTypeDefinition() == mapTypeGeneric);
            if (inf == null) continue;

            services.AddSingleton(inf, mapType);
        }
    }
}