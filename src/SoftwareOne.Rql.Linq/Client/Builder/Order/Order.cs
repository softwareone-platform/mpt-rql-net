using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

internal record Order<T, U>(Expression<Func<T, U>> Body, OrderDirection OrderDirection) : IOrder
{
    public string ToQuery()
    {
        var property = new PropertyVisitor().GetPath(Body.Body);
        return OrderDirection == OrderDirection.Descending ? $"-{property}" : property;
    }
}