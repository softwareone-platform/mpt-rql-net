using Mpt.Rql.Client;

namespace Mpt.Rql.Linq.Client.Builder.Order;

internal interface IOrderDefinitionProvider
{
    internal IList<IOrderDefinition>? GetDefinition();
}
