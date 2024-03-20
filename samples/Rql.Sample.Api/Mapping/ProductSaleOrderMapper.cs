using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using SoftwareOne.Rql;

namespace Rql.Sample.Api.Mapping
{
    public class ProductSaleOrderMapper : IRqlMapper<SalesOrderDetail, ProductSaleOrder>
    {
        public void MapEntity(IRqlMapperContext<SalesOrderDetail, ProductSaleOrder> context)
        {
            context.Map(t => t.OrderQty, t => t.OrderQty)
                .Map(t => t.SalesOrderDetailId, t => t.SalesOrderDetailId)
                .Map(t => t.SalesOrderId, t => t.SalesOrderId)
                .Map(t => t.AddressLine1, t => t.SalesOrder.BillToAddress!.AddressLine1)
                .Map(t => t.City, t => t.SalesOrder.BillToAddress!.City)
                ;
        }
    }
}
