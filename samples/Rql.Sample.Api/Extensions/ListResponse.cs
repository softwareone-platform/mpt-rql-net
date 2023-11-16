#pragma warning disable IDE0130
using System.Text.Json.Serialization;

namespace SoftwareOne.Rql;

public class ListResponse<T>
{
    [JsonPropertyName("$meta")]
    public ListResponseMetadata Metadata { get; set; } = null!;

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = null!;
}
