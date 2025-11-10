using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Abstractions.Configuration;

public class RqlSettings : IRqlSettings
{
    public RqlMappingSettings Mapping { get; init; } = new();

    public RqlSelectSettings Select { get; init; } = new();

    public RqlFilterSettings Filter { get; init; } = new();
}

public class GlobalRqlSettings : RqlSettings, IRqlGlobalSettings
{
    public RqlGeneralSettings General { get; init; } = new();
}
