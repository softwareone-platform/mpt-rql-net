using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client;

internal class OrderGenerator : IOrderGenerator
{
    public string Generate(IList<IOrder>? queryOrder) =>
        queryOrder?.Any() ?? false
            ? $"order=({string.Join(",", queryOrder.Select(e => e.ToQuery()))})"
            : string.Empty;
}