using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class ProductShapeTestExecutor : TestExecutor<ShapedProduct>
{
    protected override IRqlQueryable<ShapedProduct, ShapedProduct> MakeRql()
        => RqlFactory.Make<ShapedProduct>(services => { }, rql =>
        {
            rql.Select.Mode = RqlSelectModes.All;
            rql.Select.MaxDepth = 99;
        });

    public override IQueryable<ShapedProduct> GetQuery() => ShapedProductRepository.Query();
}