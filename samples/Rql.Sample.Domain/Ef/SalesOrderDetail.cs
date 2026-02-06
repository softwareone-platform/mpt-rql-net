using Mpt.Rql;

namespace Rql.Sample.Domain.Ef;

/// <summary>
/// Individual products associated with a specific sales order. See SalesOrderHeader.
/// </summary>
public partial class SalesOrderDetail
{
    /// <summary>
    /// Primary key. Foreign key to SalesOrderHeader.SalesOrderID.
    /// </summary>
    [RqlProperty(IsCore = true)]
    public int SalesOrderId { get; set; }

    /// <summary>
    /// Primary key. One incremental unique number per product sold.
    /// </summary>
    public int SalesOrderDetailId { get; set; }

    /// <summary>
    /// Quantity ordered per product.
    /// </summary>
    public short OrderQty { get; set; }

    /// <summary>
    /// Product sold to customer. Foreign key to Product.ProductID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Selling price of a single product.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal UnitPriceDiscount { get; set; }

    /// <summary>
    /// Per product subtotal. Computed as UnitPrice * (1 - UnitPriceDiscount) * OrderQty.
    /// </summary>
    [RqlProperty(IsCore = true)]
    public decimal LineTotal { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid Rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual SalesOrderHeader SalesOrder { get; set; } = null!;
}
