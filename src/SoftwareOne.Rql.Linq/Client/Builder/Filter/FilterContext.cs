using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Dsl;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Filter;

internal class FilterContext<T> : IFilterDefinitionProvider, IFilterContext<T> where T : class
{
    private IOperator? _operator;

    public IFilterContext<T> Eq<TValue>(Expression<Func<T, TValue>> exp, TValue value)
    {
        _operator = new Equal<T, TValue>(exp, value);
        return this;
    }

    public IFilterContext<T> NEq<TValue>(Expression<Func<T, TValue>> exp, TValue value)
    {
        _operator = new NotEqual<T, TValue>(exp, value);
        return this;
    }

    public IFilterContext<T> Gt<TValue>(Expression<Func<T, TValue>> exp, TValue value)
    {
        _operator = new Gt<T, TValue>(exp, value);
        return this;
    }

    public IFilterContext<T> Ge<TValue>(Expression<Func<T, TValue>> exp, TValue value)
    {
        _operator = new Ge<T, TValue>(exp, value);
        return this;
    }

    public IFilterContext<T> Lt<TValue>(Expression<Func<T, TValue>> exp, TValue value)
    {
        _operator = new Lt<T, TValue>(exp, value);
        return this;
    }

    public IFilterContext<T> Le<TValue>(Expression<Func<T, TValue>> exp, TValue value)
    {
        _operator = new Le<T, TValue>(exp, value);
        return this;
    }

    public IFilterContext<T> Like<TValue>(Expression<Func<T, TValue>> exp, TValue value)
    {
        _operator = new Like<T, TValue>(exp, value);
        return this;
    }

    public IFilterContext<T> In<TValue>(Expression<Func<T, TValue>> exp, IEnumerable<TValue> values)
    {
        _operator = new In<T, TValue>(exp, values);
        return this;
    }

    public IFilterContext<T> Not(IOperator op)
    {
        _operator = new NotOperator(op);
        return this;
    } 

    IOperator? IFilterDefinitionProvider.GetDefinition() => _operator;
}