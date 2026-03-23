using Mpt.Rql;

namespace Rql.Tests.Unit.Services.Models;

internal class SupportCase
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string Title { get; set; } = null!;

    public List<Parameter> Parameters { get; set; } = null!;
}
