using SoftwareOne.Rql.Client.Builder.Order;

namespace SoftwareOne.Rql.Client.RqlGenerator;

public interface IOrderGenerator
{
    string Generate(IList<IOrder>? queryOrder);
}