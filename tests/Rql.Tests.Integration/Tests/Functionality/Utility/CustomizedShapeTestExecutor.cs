using Rql.Tests.Integration.Core;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Linq.Configuration;

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

