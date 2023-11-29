using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Core
{
    internal static class RqlFactory
    {
        public static IRqlQueryable<TStorage, TStorage> Make<TStorage>(Action<ServiceCollection> configureServices, Action<RqlConfiguration> configureRql)
            => Make<TStorage, TStorage>(configureServices, configureRql);

        public static IRqlQueryable<TStorage, TView> Make<TStorage, TView>(Action<ServiceCollection> configureServices, Action<RqlConfiguration> configureRql)
         => MakeProvider(configureServices, configureRql).GetRequiredService<IRqlQueryable<TStorage, TView>>();

        public static IServiceProvider MakeProvider(Action<ServiceCollection> configureServices, Action<RqlConfiguration> configureRql)
        {
            var services = new ServiceCollection();
            configureServices(services);
            services.AddRql(configureRql);
            return services.BuildServiceProvider();
        }
    }
}
