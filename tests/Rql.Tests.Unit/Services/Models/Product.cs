namespace Mpt.Rql.Linq.UnitTests.Services.Models;

internal class Product
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    [RqlProperty(IsCore = true)]
    public Category CoreCategory { get; set; } = null!;

    public Category Category { get; set; } = null!;

    [RqlProperty(Select = RqlSelectModes.None)]
    public Category HiddenCategory { get; set; } = null!;

    [RqlProperty(IsIgnored = true)]
    public Category IgnoredCategory { get; set; } = null!;

    public List<Item> Items { get; set; } = null!;

    [RqlProperty(IsCore = true)]
    public List<Item> CoreItems { get; set; } = null!;
}
