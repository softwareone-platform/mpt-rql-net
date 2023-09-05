using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Order;

internal interface IOrderDefinitionProvider
{
    internal IList<IOrder>? GetDefinition();
}
