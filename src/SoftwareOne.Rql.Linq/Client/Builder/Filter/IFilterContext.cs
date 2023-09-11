using System.Linq.Expressions;
using SoftwareOne.Rql.Linq.Client.Builder.Operators;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IFilterContext<T> where T : class
{
    IOperator Eq<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IOperator Ge<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IOperator Gt<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IOperator In<TValue>(Expression<Func<T, TValue>> exp, IEnumerable<TValue> values);
    IOperator Le<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IOperator Like<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IOperator Lt<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IOperator Ne<TValue>(Expression<Func<T, TValue>> exp, TValue value);
    IOperator Not(IOperator inner);
    IOperator And(params IOperator[] operators);
    IOperator Or(params IOperator[] operators);
}
