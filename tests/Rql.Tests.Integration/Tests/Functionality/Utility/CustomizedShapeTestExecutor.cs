using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Configuration;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class CustomizedShapeTestExecutor : ProductShapeTestExecutor
{
    protected override IRqlQueryable<ShapedProduct, ShapedProduct> MakeRql()
        => RqlFactory.Make<ShapedProduct>(services => { });

    protected override void Customize(RqlSettings settings)
    {
        settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        settings.Select.Explicit = RqlSelectModes.All;
        settings.Select.MaxDepth = 99;
    }
}

