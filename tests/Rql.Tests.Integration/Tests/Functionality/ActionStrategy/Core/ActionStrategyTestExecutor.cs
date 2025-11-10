using Microsoft.Extensions.DependencyInjection;
using Rql.Tests.Integration.Core;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Linq.Configuration;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public class ActionStrategyTestExecutor : TestExecutor<ActionStrategyTestItem>
{
    private readonly Action<ServiceCollection> _configureServices;

    public ActionStrategyTestExecutor(Action<ServiceCollection> configureServices)
    {
        _configureServices = configureServices;
    }

    protected override IRqlQueryable<ActionStrategyTestItem, ActionStrategyTestItem> MakeRql()
        => RqlFactory.Make<ActionStrategyTestItem>(_configureServices);

    public override IQueryable<ActionStrategyTestItem> GetQuery() => ActionStrategyTestItemRepository.Query();

    protected override void Customize(IRqlSettings settings)
    {
        settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        settings.Select.Explicit = RqlSelectModes.All;
    }
}