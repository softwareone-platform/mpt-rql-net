using System;
using System.Collections.Generic;

namespace Rql.Sample.Domain.Ef;

/// <summary>
/// High-level product categorization.
/// </summary>
public partial class ProductCategory
{
    /// <summary>
    /// Primary key for ProductCategory records.
    /// </summary>
    public int ProductCategoryId { get; set; }

    /// <summary>
    /// Product category identification number of immediate ancestor category. Foreign key to ProductCategory.ProductCategoryID.
    /// </summary>
    public int? ParentProductCategoryId { get; set; }

    /// <summary>
    /// Category description.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid Rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<ProductCategory> InverseParentProductCategory { get; set; } = new List<ProductCategory>();

    public virtual ProductCategory? ParentProductCategory { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
