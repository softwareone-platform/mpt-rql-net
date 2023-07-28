using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Ordering
{
    internal interface IOrderingFunctions
    {
        MethodInfo GetOrderBy();
        MethodInfo GetThenBy();
        MethodInfo GetOrderByDescending();
        MethodInfo GetThenByDescending();
    }

    internal class OrderingFunctions<TItem, TKey> : IOrderingFunctions
    {
        public MethodInfo GetOrderBy()
        {
            Func<IQueryable<TItem>, Expression<Func<TItem, TKey>>, IOrderedQueryable<TItem>> func = Queryable.OrderBy;
            return func.Method;
        }

        public MethodInfo GetThenBy()
        {
            Func<IOrderedQueryable<TItem>, Expression<Func<TItem, TKey>>, IOrderedQueryable<TItem>> func = Queryable.ThenBy;
            return func.Method;
        }

        public MethodInfo GetOrderByDescending()
        {
            Func<IQueryable<TItem>, Expression<Func<TItem, TKey>>, IOrderedQueryable<TItem>> func = Queryable.OrderByDescending;
            return func.Method;
        }

        public MethodInfo GetThenByDescending()
        {
            Func<IOrderedQueryable<TItem>, Expression<Func<TItem, TKey>>, IOrderedQueryable<TItem>> func = Queryable.ThenByDescending;
            return func.Method;
        }
    }

}
