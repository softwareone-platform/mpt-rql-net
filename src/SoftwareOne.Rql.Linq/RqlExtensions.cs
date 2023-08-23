using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Mapping;
using SoftwareOne.Rql.Linq.Services.Ordering;
using SoftwareOne.Rql.Linq.Services.Projection;
using SoftwareOne.Rql.Parsers.Linear.Domain.Services;
using System.Reflection;

namespace SoftwareOne.Rql.Linq;

public static class RqlExtensions
{
    public static IServiceCollection AddRql(this IServiceCollection services)
        => services.AddRql(null);

    public static IServiceCollection AddRql(this IServiceCollection services, Action<RqlOptions>? configure)
    {
        var options = new RqlOptions();
        configure?.Invoke(options);

        services.AddSingleton(options.Settings);

        services.AddSingleton<IRqlParser, RqlParser>();

        services.AddScoped(typeof(IRqlQueryable<>), typeof(RqlQueryable<>));
        services.AddScoped(typeof(IRqlQueryable<,>), typeof(RqlQueryableLinq<,>));

        services.AddScoped(typeof(IMappingService<,>), typeof(MappingService<,>));
        services.AddScoped(typeof(IFilteringService<>), typeof(FilteringService<>));
        services.AddScoped(typeof(IOrderingService<>), typeof(OrderingService<>));
        services.AddScoped(typeof(IProjectionService<>), typeof(ProjectionService<>));

        services.AddSingleton<MetadataProvider>();
        services.AddSingleton<IMetadataProvider>(serviceProvider => serviceProvider.GetRequiredService<MetadataProvider>());
        services.AddSingleton<IRqlMetadataProvider>(serviceProvider => serviceProvider.GetRequiredService<MetadataProvider>());

        services.AddSingleton<IMetadataFactory, MetadataFactory>();
        services.AddSingleton(typeof(IPropertyNameProvider), options.PropertyMapperType ?? typeof(PropertyNameProvider));

        RegisterOperatorExpressions(services, options);

        if (options.ViewMappersAssembly != null)
            ScanForViewMappers(services, options.ViewMappersAssembly);

        return services;
    }

    private static void RegisterOperatorExpressions(IServiceCollection services, RqlOptions options)
    {
        var expMapping = new OperatorHandlerMapper();
        var producerType = typeof(IOperator);
        var types =
            producerType.Assembly.GetTypes().Where(t => t.IsInterface && producerType.IsAssignableFrom(t) && t != producerType).ToList();

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<ExpressionAttribute>();
            if (attribute == null)
                continue;

            if (expMapping.ContainsKey(attribute.Key))
                throw new Exception($"Expression key '{attribute.Key}' is used more than once.");

            var implementation = options.OperatorOverrides.TryGetValue(type, out var typeValue) ? typeValue : attribute.Implementation;

            services.AddSingleton(type, implementation);
            expMapping.Add(attribute.Key, type);
        }

        services.AddSingleton<IOperatorHandlerMapper>(expMapping);
        services.AddSingleton<IOperatorHandlerProvider, OperatorHandlerProvider>();
    }

    private static void ScanForViewMappers(IServiceCollection services, Assembly mappingsAssembly)
    {
        var mapType = typeof(IRqlMapper);
        var mapTypeGeneric = typeof(IRqlMapper<,>);
        var types = mappingsAssembly.GetTypes().Where(t => t.IsClass && mapType.IsAssignableFrom(t)).ToList();

        foreach (var mt in types)
        {
            var inf = mt.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == mapTypeGeneric);
            if (inf == null) continue;

            services.AddSingleton(inf, mt);
        }
    }
}