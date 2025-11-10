namespace Mpt.Rql.Abstractions.Configuration;

public class RqlSelectSettings
{
    public RqlSelectModes Implicit { get; set; } = RqlSelectModes.Core;

    public RqlSelectModes Explicit { get; set; } = RqlSelectModes.Core;

    public int? MaxDepth { get; set; }
}
