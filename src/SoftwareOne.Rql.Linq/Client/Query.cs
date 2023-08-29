#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public record Query(IOperator Op, Paging Paging, SelectFields Select, IList<IOrder>? Order = null);