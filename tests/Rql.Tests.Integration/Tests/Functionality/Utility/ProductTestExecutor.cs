using Rql.Tests.Integration.Core;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Linq.Configuration;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class ProductTestExecutor : TestExecutor<Product>
{
    protected override IRqlQueryable<Product, Product> MakeRql()
        => RqlFactory.Make<Product>(services => { });

    public override IQueryable<Product> GetQuery() => ProductRepository.Query();

    protected override void Customize(RqlSettings settings)
    {
        settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        settings.Select.Explicit = RqlSelectModes.All;
        settings.Select.MaxDepth = 10;
    }
}