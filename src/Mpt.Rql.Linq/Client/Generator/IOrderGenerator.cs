using Mpt.Rql.Linq.Client.Builder.Order;

namespace Mpt.Rql.Linq.Client.Generator;

internal interface IOrderGenerator
{
    string? Generate(IOrderDefinitionProvider? order);
}