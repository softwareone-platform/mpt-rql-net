#pragma warning disable IDE0130
using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql;

public class RqlRequest
{
    public string? Filter { get; set; }

    public string? Order { get; set; }
    
    public string? Select { get; set; }
    
    public RqlSettings? Settings { get; set; }
}
