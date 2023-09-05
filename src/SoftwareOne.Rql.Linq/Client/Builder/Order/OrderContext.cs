using SoftwareOne.Rql.Client;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Order;

internal class OrderContext<T> : IOrderDefinitionProvider, IOrderBeginContext<T>, IThenByContext<T> where T : class
{
    private IList<IOrder>? _definition;
    public IThenByContext<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpression)
    {
        Add(orderExpression, OrderDirection.Ascending);
        return this;
    }
    public IThenByContext<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpression)
    {
        Add(orderExpression, OrderDirection.Descending);
        return this;
    }

    public IThenByContext<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpression)
    {
        return OrderBy(orderExpression);
    }

    public IThenByContext<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpression)
    {
        return OrderByDescending(orderExpression);
    }

    private void Add<TValue>(Expression<Func<T, TValue>> orderExpression, OrderDirection direction)
    {
        _definition ??= new List<IOrder>();
        _definition.Add(new Order<T, TValue>(orderExpression, direction));
    }

    IList<IOrder>? IOrderDefinitionProvider.GetDefinition() => _definition;
}