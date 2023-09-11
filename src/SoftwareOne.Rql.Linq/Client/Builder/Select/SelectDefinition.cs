using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Core;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Select;

internal record SelectDefinition<T, U>(Expression<Func<T, U>> Expression) : ISelectDefinition
{
    public string ToQuery(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Expression.Body);
        return property;
    }
}