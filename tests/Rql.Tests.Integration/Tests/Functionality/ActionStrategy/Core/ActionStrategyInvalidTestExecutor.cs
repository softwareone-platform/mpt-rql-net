using Rql.Tests.Integration.Core;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Linq.Configuration;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public class ActionStrategyInvalidTestExecutor : TestExecutor<ActionStrategyInvalidTestItem>
{
    protected override IRqlQueryable<ActionStrategyInvalidTestItem, ActionStrategyInvalidTestItem> MakeRql()
        => RqlFactory.Make<ActionStrategyInvalidTestItem>(services => { });

    public override IQueryable<ActionStrategyInvalidTestItem> GetQuery() => Enumerable.Empty<ActionStrategyInvalidTestItem>().AsQueryable();

    protected override void Customize(RqlSettings settings) { }
}