using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using Mpt.Rql;

namespace Rql.Sample.Api.Mapping;

public class ProductSaleOrderMapper : IRqlMapper<SalesOrderDetail, ProductSaleOrder>
{
    public void MapEntity(IRqlMapperContext<SalesOrderDetail, ProductSaleOrder> context)
    {
        context.MapStatic(t => t.OrderQty, t => t.OrderQty)
            .MapStatic(t => t.SalesOrderDetailId, t => t.SalesOrderDetailId)
            .MapStatic(t => t.SalesOrderId, t => t.SalesOrderId)
            .MapStatic(t => t.AddressLine1, t => t.SalesOrder.BillToAddress!.AddressLine1)
            .MapStatic(t => t.City, t => t.SalesOrder.BillToAddress!.City)
            ;
    }
}
