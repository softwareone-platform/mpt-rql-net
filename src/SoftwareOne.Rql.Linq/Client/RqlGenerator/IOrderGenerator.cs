using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client;

internal interface IOrderGenerator
{
    string Generate(IList<IOrder>? queryOrder);
}