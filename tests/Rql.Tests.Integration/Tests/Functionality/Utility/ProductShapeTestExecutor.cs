using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class ProductShapeTestExecutor : TestExecutor<ShapedProduct>
{
    protected override IRqlQueryable<ShapedProduct, ShapedProduct> MakeRql()
        => RqlFactory.Make<ShapedProduct>(services => { });

    public override IQueryable<ShapedProduct> GetQuery() => ShapedProductRepository.Query();

    protected override void Customize(IRqlSettings settings)
    {
        settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        settings.Select.Explicit = RqlSelectModes.All;
        settings.Select.MaxDepth = 99;
    }
}