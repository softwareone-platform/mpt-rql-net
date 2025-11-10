#pragma warning disable IDE0130
using Mpt.Rql.Linq.Client.Builder.Request;
using System.Linq.Expressions;

namespace Mpt.Rql.Client;

public interface IRqlRequestBuilder<T> : IRqlRequestBuilder where T : class
{
    IFilteredRqlRequestBuilder<T> Where(Func<IFilterContext<T>, IOperator> filter);
    IOrderedRqlRequestBuilder<T> OrderBy<TValue>(Expression<Func<T, TValue>> order);
    IOrderedRqlRequestBuilder<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> order);
    IRqlRequestBuilder Select(Func<ISelectContext<T>, ISelectContext<T>> select);
}