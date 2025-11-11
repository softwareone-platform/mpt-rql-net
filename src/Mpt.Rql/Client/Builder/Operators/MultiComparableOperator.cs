using System.Linq.Expressions;
using Mpt.Rql.Client;
using Mpt.Rql.Client.Core;
using Mpt.Rql.Linq.Client.Builder.Operators;

namespace Mpt.Rql.Client.Builder.Operators;

internal abstract record MultiComparableOperator<T, U>(Expression<Func<T, U>> Exp, IEnumerable<U> Values) : IOperator, IComparableOperator
{
    public QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Exp.Body);
        var val = string.Join(',', Values.Select(ValueConverter.Convert));
        return new QueryOperator(property, val);
    }
}