using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests;

public class ProductTestExecutor : TestExecutor<Product>
{
    protected override void ConfigureRql(RqlOptions options)
    {
        options.Configure(cfg => cfg.DefaultActions = RqlActions.All);
    }

    protected override IQueryable<Product> GetQuery() => ProductRepository.Query();
}