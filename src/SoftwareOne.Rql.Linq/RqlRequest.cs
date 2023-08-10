#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public class RqlRequest
{
    public string? Filter { get; set; }
    public string? Order { get; set; }
    public string? Select { get; set; }
}