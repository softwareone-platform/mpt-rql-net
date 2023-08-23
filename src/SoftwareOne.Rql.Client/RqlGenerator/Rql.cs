namespace SoftwareOne.Rql.Client.RqlGenerator;

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