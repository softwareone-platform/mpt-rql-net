using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Operators;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Filter;

internal class FilterContext<T> : IFilterContext<T> where T : class
{
    public IOperator Eq<TValue>(Expression<Func<T, TValue>> exp, TValue value)
        => new Equal<T, TValue>(exp, value);

    public IOperator Ne<TValue>(Expression<Func<T, TValue>> exp, TValue value)
        => new NotEqual<T, TValue>(exp, value);

    public IOperator Gt<TValue>(Expression<Func<T, TValue>> exp, TValue value)
        => new Gt<T, TValue>(exp, value);

    public IOperator Ge<TValue>(Expression<Func<T, TValue>> exp, TValue value)
        => new Ge<T, TValue>(exp, value);

    public IOperator Lt<TValue>(Expression<Func<T, TValue>> exp, TValue value)
        => new Lt<T, TValue>(exp, value);

    public IOperator Le<TValue>(Expression<Func<T, TValue>> exp, TValue value)
        => new Le<T, TValue>(exp, value);

    public IOperator Like<TValue>(Expression<Func<T, TValue>> exp, TValue value)
        => new Like<T, TValue>(exp, value);

    public IOperator In<TValue>(Expression<Func<T, TValue>> exp, IEnumerable<TValue> values)
        => new In<T, TValue>(exp, values);
    
    public IOperator Not(IOperator inner)
        => new NotOperator(inner);

    public IOperator And(params IOperator[] operators)
        => new AndOperator(operators);

    public IOperator Or(params IOperator[] operators)
        => new OrOperator(operators);
}