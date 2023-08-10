using SoftwareOne.Rql;

namespace Rql.Sample.Contracts.Ef.Products
{
    public class ProductView
    {
        [RqlMember(MemberFlag.RegularAndReference)]
        public int Id { get; set; }
        public string? Number { get; set; }
        [RqlMember(MemberFlag.RegularAndReference)]
        public string Name { get; set; } = null!;
        public string? FileName { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public decimal ListPrice { get; set; }
        public ProductCategoryView? Category { get; set; }
        [RqlMember(MemberFlag.RegularAndReference)]
        public ProductModelView? Model { get; set; }
        public IEnumerable<ProductSaleOrder>? SaleDetails { get; set; }
    }

    public class ProductCategoryView
    {
        [RqlMember(MemberFlag.RegularAndReference)]
        public int Id { get; set; }
        [RqlMember(MemberFlag.RegularAndReference)]
        public string Name { get; set; } = null!;
        public Guid RowGuid { get; set; }
        public ProductCategoryView Parent { get; set; } = null!;
    }

    public class ProductModelView
    {
        [RqlMember(MemberFlag.RegularAndReference)]
        public int Id { get; set; }
        [RqlMember(MemberFlag.RegularAndReference)]
        public string Name { get; set; } = null!;
        public DateTime ModifiedDate { get; set; }
    }

    public class ProductSaleOrder
    {
        [RqlMember(MemberFlag.RegularAndReference)]
        public int SalesOrderId { get; set; }
        public int SalesOrderDetailId { get; set; }
        [RqlMember(MemberFlag.RegularAndReference)]
        public short OrderQty { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
    }
}