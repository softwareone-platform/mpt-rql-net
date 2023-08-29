using System.Linq.Expressions;
using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Order;

public class OrderContext<T>  where T : class
{
    private readonly IList<IOrder> _orders = new List<IOrder>();
    public OrderContext<T> OrderBy<U>(Expression<Func<T, U>> orderExpression, OrderDirection direction)
    {
        _orders.Add(new Order<T, U>(orderExpression, direction));
        return this;
    }

    internal IList<IOrder>? GetDefinition() => _orders;
}