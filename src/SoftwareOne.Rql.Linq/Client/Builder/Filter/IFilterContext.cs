using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IFilterContext<T> where T : class
{
    IFilterContext<T> Eq<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IFilterContext<T> Ge<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IFilterContext<T> Gt<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IFilterContext<T> In<TValue>(Expression<Func<T, TValue>> exp, IEnumerable<TValue> values);
    IFilterContext<T> Le<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IFilterContext<T> Like<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IFilterContext<T> Lt<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IFilterContext<T> NEq<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IFilterContext<T> Not(IOperator op);
}