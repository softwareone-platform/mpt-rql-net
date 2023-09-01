using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class ProductTestExecutor : TestExecutor<Product>
{
    protected override IRqlQueryable<Product, Product> MakeRql()
        => RqlFactory.Make<Product>();

    protected override IQueryable<Product> GetQuery() => ProductRepository.Query();
}