namespace SoftwareOne.Rql.Abstractions.Configuration;

public class RqlSelectSettings
{
    public RqlSelectModes Implicit { get; set; }

    public RqlSelectModes Explicit { get; set; }

    public int? MaxDepth { get; set; }
}
