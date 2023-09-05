using System.Text.Json.Serialization;

namespace Rql.Tests.Unit.Client.Models;

public class ExampleWithJson
{
    public string PropWithoutAttribute { get; set; } = string.Empty;
    [JsonPropertyName("IamAJsonTag")]
    public string PropWithAttribute { get; set; } = string.Empty;
    public ExampleAddressWithJson AddressWithOutAttribute { get; set; } = new ExampleAddressWithJson();
    [JsonPropertyName("Address")]
    public ExampleAddressWithJson AddressWithAttribute { get; set; } = new ExampleAddressWithJson();
}