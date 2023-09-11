using System.Linq.Expressions;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Core;

namespace SoftwareOne.Rql.Linq.Client.Builder.Operators;

internal abstract record ComparableOperator<T, U>(Expression<Func<T, U>> Exp, U Value) : IOperator, IComparableOperator where T : class
{
    public QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Exp.Body);
        return new QueryOperator(property, ValueConverter.Convert(Value));
    }
}