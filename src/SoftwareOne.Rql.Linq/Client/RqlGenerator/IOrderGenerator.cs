using SoftwareOne.Rql.Linq.Client.Builder.Order;

namespace SoftwareOne.Rql.Linq.Client.RqlGenerator;

public interface IOrderGenerator
{
    string Generate(IList<IOrder>? queryOrder);
}