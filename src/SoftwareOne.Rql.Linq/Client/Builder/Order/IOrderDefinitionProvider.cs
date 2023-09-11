using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Order;

internal interface IOrderDefinitionProvider
{
    internal IList<IOrderDefinition>? GetDefinition();
}
