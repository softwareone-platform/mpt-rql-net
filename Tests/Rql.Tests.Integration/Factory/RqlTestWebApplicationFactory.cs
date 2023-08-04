using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Rql.Sample.Application.Common.Interfaces.Persistence.InMemory;
using Rql.Tests.Integration.Mock;

namespace Rql.Tests.Integration.Factory;

public class RqlTestWebApplicationFactory : WebApplicationFactory<Rql.Sample.Api.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ISampleRepository, MockProductRepository>();
        });
    }
}