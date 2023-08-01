using System;
using System.Collections.Generic;

namespace Rql.Sample.Domain.Ef;

/// <summary>
/// Cross-reference table mapping product descriptions and the language the description is written in.
/// </summary>
public partial class ProductModelProductDescription
{
    /// <summary>
    /// Primary key. Foreign key to ProductModel.ProductModelID.
    /// </summary>
    public int ProductModelId { get; set; }

    /// <summary>
    /// Primary key. Foreign key to ProductDescription.ProductDescriptionID.
    /// </summary>
    public int ProductDescriptionId { get; set; }

    /// <summary>
    /// The culture for which the description is written
    /// </summary>
    public string Culture { get; set; } = null!;

    public Guid Rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    public virtual ProductDescription ProductDescription { get; set; } = null!;

    public virtual ProductModel ProductModel { get; set; } = null!;
}
