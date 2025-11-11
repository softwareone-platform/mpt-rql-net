using Mpt.Rql.Client.Builder.Order;

namespace Mpt.Rql.Client.Generator;

internal interface IOrderGenerator
{
    string? Generate(IOrderDefinitionProvider? order);
}