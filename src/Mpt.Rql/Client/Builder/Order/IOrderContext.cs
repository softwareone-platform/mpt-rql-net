using System.Linq.Expressions;

namespace Mpt.Rql.Client.Builder.Order;

internal interface IOrderContext<T> where T : class
{
    void AddOrder<TValue>(Expression<Func<T, TValue>> orderExpression, OrderDirection direction);
}