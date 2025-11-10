using Newtonsoft.Json;
using Mpt.Rql;

namespace Rql.Tests.Integration.Core;

public class Product : ITestEntity
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    public string? Desc { get; set; }

    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public DateTime ListDate { get; set; }

    public decimal Price { get; set; }

    public decimal SellPrice { get; set; }

    public Product Reference { get; set; } = null!;

    public List<Product> Collection { get; set; } = null!;

    public List<ProductOrder> Orders { get; set; } = null!;

    public List<int> OrdersIds { get; set; } = null!;

    [RqlProperty(IsIgnored = true)]
    public bool Ignored { get; set; }
}

public class ProductOrder
{
    public int Id { get; set; }
    public string ClientName { get; set; } = null!;
}
