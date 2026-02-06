using Mpt.Rql;

namespace Rql.Sample.Domain.Ef;

public partial class ProductModel
{
    [RqlProperty(IsCore = true)]
    public int ProductModelId { get; set; }

    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    public string? CatalogDescription { get; set; }

    public Guid Rowguid { get; set; }

    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<ProductModelProductDescription> ProductModelProductDescriptions { get; set; } = [];

    public virtual ICollection<Product> Products { get; set; } = [];
}
