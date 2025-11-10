using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using SoftwareOne.Rql;

namespace Rql.Sample.Api.Mapping;

internal class ProductMapper : IRqlMapper<Product, ProductView>
{
    public void MapEntity(IRqlMapperContext<Product, ProductView> context)
    {
        context.MapStatic(t => t.Id, t => t.ProductId)
            .MapDynamic(t => t.Model, t => t.ProductModel)
            .MapDynamic(t => t.SaleDetails, t => t.SalesOrderDetails)
            .MapStatic(t => t.SaleDetailIds, t => t.SalesOrderDetails.Select(s => s.SalesOrderDetailId));
    }
}
