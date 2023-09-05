using SoftwareOne.Rql.Linq.Client.Order;

namespace SoftwareOne.Rql.Linq.Client;

internal interface IOrderGenerator
{
    string? Generate(IOrderDefinitionProvider? order);
}