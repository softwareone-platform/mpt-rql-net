using SoftwareOne.Rql.Linq.Client.Builder.Order;

namespace SoftwareOne.Rql.Linq.Client.Generator;

internal interface IOrderGenerator
{
    string? Generate(IOrderDefinitionProvider? order);
}