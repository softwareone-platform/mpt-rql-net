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
            rql.Settings.Select.Mode = RqlSelectMode.All;
        });

    public override IQueryable<ActionStrategyTestItem> GetQuery() => ActionStrategyTestItemRepository.Query();
}