using SoftwareOne.Rql.Linq.Client.Builder.Dsl;
using SoftwareOne.Rql.Linq.Client.Builder.Order;
using SoftwareOne.Rql.Linq.Client.Builder.Paging;
using SoftwareOne.Rql.Linq.Client.Builder.Select;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public record Query(IOperator Op, Paging Paging, SelectFields Select, IList<IOrder>? Order = null);