namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public static class ActionStrategyTestItemRepository
{
    private static readonly List<ActionStrategyTestItem> _data;

    static ActionStrategyTestItemRepository()
    {
        _data =
        [
            new() { Id = 1, All = new(), FilterOnly = new (), Nothing = new(), OrderOnly = new (), SelectOnly = new() },
            new() { Id = 2, All = new(), FilterOnly = new (), Nothing = new(), OrderOnly = new (), SelectOnly = new() },
            new() { Id = 3, All = new(), FilterOnly = new (), Nothing = new(), OrderOnly = new (), SelectOnly = new() }
        ];
    }

    public static IQueryable<ActionStrategyTestItem> Query() => _data.Select(s => s).AsQueryable();
}