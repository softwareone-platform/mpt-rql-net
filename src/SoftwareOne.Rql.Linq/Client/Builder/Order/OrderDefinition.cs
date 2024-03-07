using SoftwareOne.Rql.Client;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Order;

internal record OrderDefinition<T, TValue>(Expression<Func<T, TValue>> Body, OrderDirection OrderDirection) : IOrderDefinition
{
    public string ToQuery(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Body.Body);
        return OrderDirection == OrderDirection.Descending ? $"-{property}" : property;
    }
}