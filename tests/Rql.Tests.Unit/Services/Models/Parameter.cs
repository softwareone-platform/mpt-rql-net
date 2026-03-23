using Mpt.Rql;

namespace Rql.Tests.Unit.Services.Models;

internal class Parameter
{
    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    [RqlProperty(IsCore = true)]
    public string? Value { get; set; }
}
