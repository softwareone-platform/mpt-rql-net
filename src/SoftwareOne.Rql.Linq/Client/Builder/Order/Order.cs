using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Order;

internal record Order<T, TValue>(Expression<Func<T, TValue>> Body, OrderDirection OrderDirection) : IInternalOrder
{
    public string ToQuery(IPropertyVisitor propertyVisitor)
    {
        var property = propertyVisitor.GetPath(Body.Body);
        return OrderDirection == OrderDirection.Descending ? $"-{property}" : property;
    }
}