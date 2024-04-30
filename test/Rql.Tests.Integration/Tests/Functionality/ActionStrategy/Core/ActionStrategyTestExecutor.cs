using Microsoft.Extensions.DependencyInjection;
using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public class ActionStrategyTestExecutor : TestExecutor<ActionStrategyTestItem>
{
    private readonly Action<ServiceCollection> _configureServices;

    public ActionStrategyTestExecutor(Action<ServiceCollection> configureServices)
    {
        _configureServices = configureServices;
    }

    protected override IRqlQueryable<ActionStrategyTestItem, ActionStrategyTestItem> MakeRql()
        => RqlFactory.Make<ActionStrategyTestItem>(_configureServices, rql =>
        {
            rql.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Select.Explicit = RqlSelectModes.All;
        });

    public override IQueryable<ActionStrategyTestItem> GetQuery() => ActionStrategyTestItemRepository.Query();
}