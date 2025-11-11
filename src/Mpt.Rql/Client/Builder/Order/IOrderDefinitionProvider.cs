using Mpt.Rql.Client;

namespace Mpt.Rql.Client.Builder.Order;

internal interface IOrderDefinitionProvider
{
    internal IList<IOrderDefinition>? GetDefinition();
}
