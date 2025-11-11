using Mpt.Rql;

namespace Rql.Tests.Unit.Services.Models;

internal class Category
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public List<Product> Products { get; set; } = null!;
}
