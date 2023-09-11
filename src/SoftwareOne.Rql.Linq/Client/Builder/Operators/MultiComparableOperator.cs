using System.Linq.Expressions;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Core;

namespace SoftwareOne.Rql.Linq.Client.Builder.Operators;

internal abstract record MultiComparableOperator<T, U>(Expression<Func<T, U>> Exp, IEnumerable<U> Values) : IOperator, IComparableOperator
{
    public QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Exp.Body);
        var val = string.Join(',', Values.Select(ValueConverter.Convert));
        return new QueryOperator(property, val);
    }
}