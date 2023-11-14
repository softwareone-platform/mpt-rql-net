using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class ProductShapeTestExecutor : TestExecutor<ShapedProduct>
{
    protected override IRqlQueryable<ShapedProduct, ShapedProduct> MakeRql()
        => RqlFactory.Make<ShapedProduct>(t =>
        {
            t.Settings.Select.Mode = RqlSelectMode.All;
            t.Settings.Select.MaxDepth = 99;
        });

    public override IQueryable<ShapedProduct> GetQuery() => ShapedProductRepository.Query();
}