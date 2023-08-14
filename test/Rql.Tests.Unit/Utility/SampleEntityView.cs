namespace Rql.Tests.Unit.Utility;

public class SampleEntityView
{
    public int Id { get; set; }
    public string? Desc { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public DateTime ListDate { get; set; }
    public decimal Price { get; set; }
    public decimal SellPrice { get; set; }
    public SampleEntityView? Sub { get; set; }
}