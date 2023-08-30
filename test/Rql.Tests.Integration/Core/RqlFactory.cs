using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq;

namespace Rql.Tests.Integration.Core
{
    internal static class RqlFactory
    {
        public static IRqlQueryable<T> Make<T>(Action<RqlOptions>? configure)
        {
            var services = new ServiceCollection();
            services.AddRql(configure);
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IRqlQueryable<T>>();
        }
    }
}
