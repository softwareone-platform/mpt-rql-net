using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Configuration;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class CustomizedShapeTestExecutor : ProductShapeTestExecutor
{
    protected override IRqlQueryable<ShapedProduct, ShapedProduct> MakeRql()
        => RqlFactory.Make<ShapedProduct>(services => { }, rql =>
        {
            rql.Select.Mode = RqlSelectModes.None;
            rql.Select.MaxDepth = 0;
        });

    protected override RqlCustomization? GetCustomisation() => new()
    {
        Select = new RqlSelectSettings
        {
            Mode = RqlSelectModes.All,
            MaxDepth = 99
        }
    };
}

