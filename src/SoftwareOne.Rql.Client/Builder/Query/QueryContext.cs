using System.Linq.Expressions;
using SoftwareOne.Rql.Client.Builder.Dsl;

namespace SoftwareOne.Rql.Client.Builder.Query;

public class QueryContext<T> where T : class
{
    public IOperator Eq<U>(Expression<Func<T, U>> exp, U value) => new Equal<T, U>(exp, value);
    public IOperator NEq<U>(Expression<Func<T, U>> exp, U value) => new NotEqual<T, U>(exp, value);
    public IOperator Gt<U>(Expression<Func<T, U>> exp, U value) => new Gt<T, U>(exp, value);
    public IOperator Ge<U>(Expression<Func<T, U>> exp, U value) => new Ge<T, U>(exp, value);
    public IOperator Lt<U>(Expression<Func<T, U>> exp, U value) => new Lt<T, U>(exp, value);
    public IOperator Le<U>(Expression<Func<T, U>> exp, U value) => new Le<T, U>(exp, value);
    public IOperator Like<U>(Expression<Func<T, U>> exp, U value) => new Like<T, U>(exp, value);
    public IOperator In<U>(Expression<Func<T, U>> exp, IEnumerable<U> values) => new In<T, U>(exp, values);
    public IOperator Not(IOperator op) => new NotOperator(op);
}