#pragma warning disable IDE0130
namespace Mpt.Rql;

public class PaginationMetadata
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
}
