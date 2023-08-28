#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public record Rql(string Query, string Select, string Paging, string Order)
{
    public override string ToString()
    {
        return string.Join(
            '&', 
            new List<string> { Query, Select, Paging, Order }
                .Where(part => !string.IsNullOrWhiteSpace(part))
            );
    }
}