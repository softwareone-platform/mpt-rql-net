using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Rql.Tests.Integration.Core;

namespace Rql.Tests.Integration.Tests.Functionality.Utility;

public class CaseInsensitiveProductTestExecutor : TestExecutor<Product>
{
    protected override IRqlQueryable<Product, Product> MakeRql()
        => RqlFactory.Make<Product>(services => { });

    public override IQueryable<Product> GetQuery() => ProductRepository.Query();

    protected override void Customize(IRqlSettings settings)
    {
        settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        settings.Select.Explicit = RqlSelectModes.All;
        settings.Select.MaxDepth = 10;
        
        // Enable case insensitive string comparisons
        settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
        settings.Filter.Strings.Strategy = StringComparisonStrategy.Default;
    }
}