using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public class ActionStrategyInvalidTestExecutor : TestExecutor<ActionStrategyInvalidTestItem>
{
    protected override IRqlQueryable<ActionStrategyInvalidTestItem, ActionStrategyInvalidTestItem> MakeRql()
        => RqlFactory.Make<ActionStrategyInvalidTestItem>(services => { }, rql => { });

    public override IQueryable<ActionStrategyInvalidTestItem> GetQuery() => Enumerable.Empty<ActionStrategyInvalidTestItem>().AsQueryable();
}