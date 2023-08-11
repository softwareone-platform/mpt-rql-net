using SoftwareOne.Rql;
using SoftwareOne.Rql.Extensions.Core;

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection
{
    public static class RqlExtensions
    {
        public static IServiceCollection AddSoftwareOneRql(this IServiceCollection services)
            => services.AddSoftwareOneRql(null);

        public static IServiceCollection AddSoftwareOneRql(this IServiceCollection services, Action<RqlOptions>? configure)
        {
            services.AddScoped<IErrorResultProvider, ErrorResultProvider>();
            services.AddScoped(typeof(IRqlRequest<>), typeof(RqlRequest<>));
            services.AddScoped(typeof(IRqlRequest<,>), typeof(RqlRequest<,>));
            services.AddHttpContextAccessor();

            return SoftwareOne.Rql.Linq.RqlExtensions.AddRql(services, options =>
            {
                options.Configure(rqlSettings =>
                {
                    rqlSettings.Select.MaxDepth = 1;
                    rqlSettings.Select.Mode = SelectMode.All;
                });
                configure?.Invoke(options);
            });
        }

    }
}
