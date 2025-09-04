using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Abstractions.Configuration;
using SoftwareOne.Rql.Linq.Configuration;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public class ActionStrategyInvalidTestExecutor : TestExecutor<ActionStrategyInvalidTestItem>
{
    protected override IRqlQueryable<ActionStrategyInvalidTestItem, ActionStrategyInvalidTestItem> MakeRql()
        => RqlFactory.Make<ActionStrategyInvalidTestItem>(services => { });

    public override IQueryable<ActionStrategyInvalidTestItem> GetQuery() => Enumerable.Empty<ActionStrategyInvalidTestItem>().AsQueryable();

    protected override void Customize(RqlSettings settings) { }
}