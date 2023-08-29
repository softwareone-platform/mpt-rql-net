using System.Linq.Expressions;
using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Dsl;

internal abstract record MultiComparableOperator<T, U>(Expression<Func<T, U>> Exp, IEnumerable<U> Values) : Operator, IComparableOperator
{
    public QueryOperator ToQueryOperator()
    {
        var property = new PropertyVisitor().GetPath(Exp.Body);
        var val = string.Join(',', Values.Select(ValueConverter.Convert));
        return new QueryOperator(property, val);
    }
}