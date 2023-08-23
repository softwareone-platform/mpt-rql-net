using SoftwareOne.Rql.Client.Builder.Dsl;
using SoftwareOne.Rql.Client.Builder.Order;
using SoftwareOne.Rql.Client.Builder.Paging;
using SoftwareOne.Rql.Client.Builder.Select;

namespace SoftwareOne.Rql.Client;


public record Query(IOperator Op, Paging Paging, SelectFields Select, IList<IOrder>? Order = null);