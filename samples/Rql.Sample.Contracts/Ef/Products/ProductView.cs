using SoftwareOne.Rql;

namespace Rql.Sample.Contracts.Ef.Products
{
    public class ProductView
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        public string? Number { get; set; }

        [RqlProperty(IsCore = true)]
        public string Name { get; set; } = null!;

        public string? FileName { get; set; } = null!;

        public DateTime Date { get; set; }

        public decimal Price { get; set; }

        public decimal ListPrice { get; set; }
        public ViewProductStatus Status { get; set; }

        public ProductCategoryView? Category { get; set; }

        [RqlProperty(IsCore = true)]
        public ProductModelView? Model { get; set; }

        [RqlProperty(IsHidden = true)]
        public IEnumerable<ProductSaleOrder>? SaleDetails { get; set; }

        [RqlProperty(IsHidden = true)]
        public IEnumerable<int>? SaleDetailIds { get; set; }
    }

    public enum ViewProductStatus
    {
        Draft = 0,
        Active = 1,
        Updating = 2,
        Unlisted = 3
    }

    public class ProductCategoryView
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(IsCore = true)]
        public string Name { get; set; } = null!;

        public Guid RowGuid { get; set; }

        public ProductCategoryView Parent { get; set; } = null!;
    }

    public class ProductModelView
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(IsCore = true)]
        public string Name { get; set; } = null!;

        public DateTime ModifiedDate { get; set; }
    }

    public class ProductSaleOrder
    {
        [RqlProperty(IsCore = true)]
        public int SalesOrderId { get; set; }

        public int SalesOrderDetailId { get; set; }

        [RqlProperty(IsCore = true)]
        public short OrderQty { get; set; }

        public string? City { get; set; }

        public string? AddressLine1 { get; set; }
    }
}