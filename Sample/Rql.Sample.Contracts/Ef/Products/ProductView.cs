namespace Rql.Sample.Contracts.Ef.Products
{
    public class ProductView
    {
        public int Id { get; set; }
        public string? Number { get; set; }
        public string Name { get; set; } = null!;
        public string? FileName { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public decimal ListPrice { get; set; }
        public ProductCategoryView? Category { get; set; }
        public ProductModelView? Model { get; set; }
        public IEnumerable<ProductSaleOrder>? SaleDetails { get; set; }
    }

    public class ProductCategoryView
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ProductModelView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public class ProductSaleOrder
    {
        public int SalesOrderId { get; set; }
        public int SalesOrderDetailId { get; set; }
        public short OrderQty { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
    }
}