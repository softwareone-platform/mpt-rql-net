using Mpt.Rql.Client;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Client.Builder.Select;

internal record SelectDefinition<T, U>(Expression<Func<T, U>> Expression) : ISelectDefinition
{
    public string ToQuery(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Expression.Body);
        return property;
    }
}