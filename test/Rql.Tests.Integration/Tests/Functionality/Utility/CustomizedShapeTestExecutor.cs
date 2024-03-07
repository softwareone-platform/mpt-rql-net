using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Configuration;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class CustomizedShapeTestExecutor : ProductShapeTestExecutor
{
    protected override IRqlQueryable<ShapedProduct, ShapedProduct> MakeRql()
        => RqlFactory.Make<ShapedProduct>(services => { }, rql =>
        {
            rql.Select.Mode = RqlSelectMode.None;
            rql.Select.MaxDepth = 0;
        });

    protected override RqlCustomization? GetCustomisation() => new()
    {
        Select = new RqlSelectSettings
        {
            Mode = RqlSelectMode.All,
            MaxDepth = 99
        }
    };
}

