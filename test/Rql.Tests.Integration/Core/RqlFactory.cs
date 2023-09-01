using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql;
using System;

namespace Rql.Tests.Integration.Core
{
    internal static class RqlFactory
    {
        public static IRqlQueryable<TStorage, TStorage> Make<TStorage>(Action<RqlConfiguration>? configure = null)
            => Make<TStorage, TStorage>(configure);

        public static IRqlQueryable<TStorage, TView> Make<TStorage, TView>(Action<RqlConfiguration>? configure)
         => MakeProvider(configure).GetRequiredService<IRqlQueryable<TStorage, TView>>();

        public static IServiceProvider MakeProvider(Action<RqlConfiguration>? configure = null)
        {
            var services = new ServiceCollection();
            if (configure != null)
                services.AddRql(configure);
            else
                services.AddRql(); // keep it explicit for default case coverage
            return services.BuildServiceProvider();
        }
    }
}
