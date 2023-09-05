using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Order;

internal interface IInternalOrder : IOrder
{
    string ToQuery(IPropertyVisitor propertyVisitor);
}