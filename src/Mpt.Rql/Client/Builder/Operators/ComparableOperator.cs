using Mpt.Rql.Client.Core;
using Mpt.Rql.Linq.Client.Builder.Operators;
using System.Linq.Expressions;

namespace Mpt.Rql.Client.Builder.Operators;

internal abstract record ComparableOperator<T, U>(Expression<Func<T, U>> Exp, U Value) : IOperator, IComparableOperator where T : class
{
    public QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Exp.Body);
        return new QueryOperator(property, ValueConverter.Convert(Value));
    }
}