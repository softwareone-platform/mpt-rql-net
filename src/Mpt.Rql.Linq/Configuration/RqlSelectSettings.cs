using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Linq.Configuration;

internal record RqlSelectSettings : IRqlSelectSettings
{
    public RqlSelectModes Implicit { get; set; } = RqlSelectModes.Core;

    public RqlSelectModes Explicit { get; set; } = RqlSelectModes.Core;

    public int? MaxDepth { get; set; }
}