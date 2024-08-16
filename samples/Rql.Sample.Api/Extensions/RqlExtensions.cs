
using Rql.Sample.Api.Extensions.Core;
using SoftwareOne.Rql;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;

public static class RqlExtensions
{
    public static IServiceCollection AddSoftwareOneRql(this IServiceCollection services)
        => services.AddSoftwareOneRql(null);

    public static IServiceCollection AddSoftwareOneRql(this IServiceCollection services, Action<RqlConfiguration>? configure)
    {
        services.AddScoped<IErrorResultProvider, ErrorResultProvider>();
        services.AddScoped(typeof(IRqlRequest<>), typeof(RqlRequest<>));
        services.AddScoped(typeof(IRqlRequest<,>), typeof(RqlRequest<,>));
        services.AddHttpContextAccessor();

        return services.AddRql(config =>
        {
            config.Settings.Select.MaxDepth = 1;
            config.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            config.Settings.Select.Explicit = RqlSelectModes.All;
            configure?.Invoke(config);
        });
    }
}
