using System.Linq.Expressions;
using SoftwareOne.Rql.Linq.Client.Builder.Order;

namespace SoftwareOne.Rql.Linq.Client.Order;

internal interface IOrderContext<T> where T : class
{
    void AddOrder<TValue>(Expression<Func<T, TValue>> orderExpression, OrderDirection direction);
}