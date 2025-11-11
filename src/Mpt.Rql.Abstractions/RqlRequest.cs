#pragma warning disable IDE0130
namespace Mpt.Rql;

public record RqlRequest
{
    public string? Filter { get; set; }

    public string? Order { get; set; }

    public string? Select { get; set; }
}
