#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public class RqlAuditContext
{
    public RqlAuditContext()
    {
        Omitted = new List<string>();
        Invisible = new List<string>();
    }

    public List<string> Omitted { get; init; }
    public List<string> Invisible { get; init; }
}