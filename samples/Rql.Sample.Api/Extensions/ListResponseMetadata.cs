#pragma warning disable IDE0130
using System.Text.Json.Serialization;

namespace Mpt.Rql;

public class ListResponseMetadata
{
    [JsonPropertyName("pagination")]
    public PaginationMetadata Pagination { get; set; } = null!;

    [JsonPropertyName("omitted")]
    public List<string> Omitted { get; set; } = null!;
}