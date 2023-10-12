using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using SoftwareOne.Rql;
using System.Linq.Expressions;

namespace Rql.Sample.Api.Mapping
{
    internal class ProductMapper : IRqlMapper<Product, ProductView>
    {
        public Expression<Func<Product, ProductView>> GetMapping()
          => (t) => new ProductView
          {
              Id = t.ProductId,
              Number = t.ProductNumber,
              Name = t.Name,
              FileName = t.ThumbnailPhotoFileName,
              Price = t.StandardCost,
              ListPrice = t.ListPrice,
              Date = t.SellStartDate,
              Status = (ViewProductStatus)t.Status,
              Category = new ProductCategoryView
              {
                  Id = t.ProductCategory!.ProductCategoryId,
                  Name = t.ProductCategory.Name,
                  RowGuid = t.ProductCategory.Rowguid,
                  Parent = new ProductCategoryView
                  {
                      Id = t.ProductCategory.ParentProductCategory!.ProductCategoryId,
                      Name = t.ProductCategory.ParentProductCategory.Name,
                      RowGuid = t.ProductCategory.ParentProductCategory.Rowguid,
                  }
              },
              Model = new ProductModelView
              {
                  Id = t.ProductModel!.ProductModelId,
                  Name = t.ProductModel.Name,
                  ModifiedDate = t.ProductModel.ModifiedDate,
              },
              SaleDetails = t.SalesOrderDetails.Select(s => new ProductSaleOrder
              {
                  OrderQty = s.OrderQty,
                  SalesOrderDetailId = s.SalesOrderDetailId,
                  SalesOrderId = s.SalesOrderId,
                  AddressLine1 = s.SalesOrder.BillToAddress!.AddressLine1,
                  City = s.SalesOrder.BillToAddress.City
              })
          };
    }
}
