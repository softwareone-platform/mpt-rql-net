namespace Rql.Sample.Domain.Ef;

/// <summary>
/// General sales order information.
/// </summary>
public partial class SalesOrderHeader
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int SalesOrderId { get; set; }

    /// <summary>
    /// Incremental number to track changes to the sales order over time.
    /// </summary>
    public byte RevisionNumber { get; set; }

    /// <summary>
    /// Dates the sales order was created.
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Date the order is due to the customer.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Date the order was shipped to the customer.
    /// </summary>
    public DateTime? ShipDate { get; set; }

    /// <summary>
    /// Order current status. 1 = In process; 2 = Approved; 3 = Backordered; 4 = Rejected; 5 = Shipped; 6 = Cancelled
    /// </summary>
    public byte Status { get; set; }

    /// <summary>
    /// 0 = Order placed by sales person. 1 = Order placed online by customer.
    /// </summary>
    public bool? OnlineOrderFlag { get; set; }

    /// <summary>
    /// Unique sales order identification number.
    /// </summary>
    public string SalesOrderNumber { get; set; } = null!;

    /// <summary>
    /// Customer purchase order number reference. 
    /// </summary>
    public string? PurchaseOrderNumber { get; set; }

    /// <summary>
    /// Financial accounting number reference.
    /// </summary>
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Customer identification number. Foreign key to Customer.CustomerID.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// The ID of the location to send goods.  Foreign key to the Address table.
    /// </summary>
    public int? ShipToAddressId { get; set; }

    /// <summary>
    /// The ID of the location to send invoices.  Foreign key to the Address table.
    /// </summary>
    public int? BillToAddressId { get; set; }

    /// <summary>
    /// Shipping method. Foreign key to ShipMethod.ShipMethodID.
    /// </summary>
    public string ShipMethod { get; set; } = null!;

    /// <summary>
    /// Approval code provided by the credit card company.
    /// </summary>
    public string? CreditCardApprovalCode { get; set; }

    /// <summary>
    /// Sales subtotal. Computed as SUM(SalesOrderDetail.LineTotal)for the appropriate SalesOrderID.
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal TaxAmt { get; set; }

    /// <summary>
    /// Shipping cost.
    /// </summary>
    public decimal Freight { get; set; }

    /// <summary>
    /// Total due from customer. Computed as Subtotal + TaxAmt + Freight.
    /// </summary>
    public decimal TotalDue { get; set; }

    /// <summary>
    /// Sales representative comments.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.
    /// </summary>
    public Guid Rowguid { get; set; }

    /// <summary>
    /// Date and time the record was last updated.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    public virtual Address? BillToAddress { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = [];

    public virtual Address? ShipToAddress { get; set; }
}
