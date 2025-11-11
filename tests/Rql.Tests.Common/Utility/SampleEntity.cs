namespace Mpt.UnitTests.Common.Utility;

internal class SampleEntity
{
    public int Id { get; set; }
    public string Category { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string ProductNumber { get; set; } = null!;
    public bool? MakeFlag { get; set; }
    public bool? FinishedGoodsFlag { get; set; }
    public decimal StandardCost { get; set; }
    public decimal ListPrice { get; set; }
    public string? Size { get; set; }
    public decimal? Weight { get; set; }
    public int DaysToManufacture { get; set; }
    public DateTime ListDate { get; set; }
    public Guid Rowguid { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int Year { get; set; }
    public decimal Price { get; set; }
    public decimal SalePrice { get; set; }
    public ICollection<ProductType>? Types { get; set; }
}

public class ProductType
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
}
