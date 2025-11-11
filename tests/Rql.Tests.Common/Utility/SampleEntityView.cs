namespace Rql.Tests.Common.Utility;

public class SampleEntityView
{
    public virtual int Id { get; set; }
    public virtual string? Desc { get; set; }
    public virtual string Name { get; set; } = null!;
    public virtual string Category { get; set; } = null!;
    public virtual DateTime ListDate { get; set; }
    public virtual decimal Price { get; set; }
    public virtual decimal SellPrice { get; set; }
    public virtual SampleEntityView? Sub { get; set; }
}