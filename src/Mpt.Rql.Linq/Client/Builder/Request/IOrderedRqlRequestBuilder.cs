#pragma warning disable IDE0130
using Mpt.Rql.Linq.Client.Builder.Request;
using System.Linq.Expressions;

namespace Mpt.Rql.Client;

public interface IOrderedRqlRequestBuilder<T>  : IRqlRequestBuilder where T : class
{
    IOrderedRqlRequestBuilder<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpression);
    IOrderedRqlRequestBuilder<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpression);
    IRqlRequestBuilder Select(Func<ISelectContext<T>, ISelectContext<T>> selectFunc);
}