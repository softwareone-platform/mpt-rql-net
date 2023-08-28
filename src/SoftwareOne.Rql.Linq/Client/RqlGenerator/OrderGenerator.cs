using SoftwareOne.Rql.Linq.Client.Builder.Order;

namespace SoftwareOne.Rql.Linq.Client.RqlGenerator;

public class OrderGenerator : IOrderGenerator
{
    public string Generate(IList<IOrder>? queryOrder) =>
        queryOrder?.Any() ?? false
            ? $"order=({string.Join(",", queryOrder.Select(e => e.ToQuery()))})"
            : string.Empty;
}