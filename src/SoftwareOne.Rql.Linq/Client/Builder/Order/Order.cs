using System.Linq.Expressions;
using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Order;

public record Order<T, U>(Expression<Func<T, U>> Body, OrderDirection OrderDirection) : IOrder
{
    public string ToQuery()
    {
        var property = new PropertyVisitor().GetPath(Body.Body);
        return OrderDirection == OrderDirection.Descending ? $"-{property}" : property;
    }
}