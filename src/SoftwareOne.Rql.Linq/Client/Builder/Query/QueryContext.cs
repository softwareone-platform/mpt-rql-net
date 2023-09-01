using System.Linq.Expressions;
using SoftwareOne.Rql.Linq.Client.Dsl;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public class QueryContext<T> where T : class
{
    public IOperator Eq<TValue>(Expression<Func<T, TValue>> exp, TValue value) => new Equal<T, TValue>(exp, value);
    public IOperator NEq<TValue>(Expression<Func<T, TValue>> exp, TValue value) => new NotEqual<T, TValue>(exp, value);
    public IOperator Gt<TValue>(Expression<Func<T, TValue>> exp, TValue value) => new Gt<T, TValue>(exp, value);
    public IOperator Ge<TValue>(Expression<Func<T, TValue>> exp, TValue value) => new Ge<T, TValue>(exp, value);
    public IOperator Lt<TValue>(Expression<Func<T, TValue>> exp, TValue value) => new Lt<T, TValue>(exp, value);
    public IOperator Le<TValue>(Expression<Func<T, TValue>> exp, TValue value) => new Le<T, TValue>(exp, value);
    public IOperator Like<TValue>(Expression<Func<T, TValue>> exp, TValue value) => new Like<T, TValue>(exp, value);

    public IOperator In<TValue>(Expression<Func<T, TValue>> exp, IEnumerable<TValue> values) =>
        new In<T, TValue>(exp, values);

    public IOperator Not(IOperator op) => new NotOperator(op);
}