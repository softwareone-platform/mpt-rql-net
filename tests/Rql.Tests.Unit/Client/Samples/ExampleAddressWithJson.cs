using System.Text.Json.Serialization;

namespace Rql.Tests.Unit.Client.Samples;

public class ExampleAddressWithJson
{
    [JsonPropertyName("Street")]
    public string StreetWithProp { get; set; } = string.Empty;

    public string CityWithoutProp { get; set; } = string.Empty;
}