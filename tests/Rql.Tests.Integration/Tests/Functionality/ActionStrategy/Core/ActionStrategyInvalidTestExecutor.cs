using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public class ActionStrategyInvalidTestExecutor : TestExecutor<ActionStrategyInvalidTestItem>
{
    protected override IRqlQueryable<ActionStrategyInvalidTestItem, ActionStrategyInvalidTestItem> MakeRql()
        => RqlFactory.Make<ActionStrategyInvalidTestItem>(services => { });

    public override IQueryable<ActionStrategyInvalidTestItem> GetQuery() => Enumerable.Empty<ActionStrategyInvalidTestItem>().AsQueryable();

    protected override void Customize(IRqlSettings settings) { }
}