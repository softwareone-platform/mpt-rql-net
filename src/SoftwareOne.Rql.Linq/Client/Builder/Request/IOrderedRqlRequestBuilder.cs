#pragma warning disable IDE0130
using SoftwareOne.Rql.Linq.Client.Builder.Request;
using System.Linq.Expressions;
using SoftwareOne.Rql.Linq.Client.Request;

namespace SoftwareOne.Rql.Client;

public interface IOrderedRqlRequestBuilder<T>  : IRqlRequestBuilder where T : class
{
    IOrderedRqlRequestBuilder<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpression);
    IOrderedRqlRequestBuilder<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpression);
    IRqlRequestBuilder Select(Func<ISelectContext<T>, ISelectContext<T>> selectFunc);
}