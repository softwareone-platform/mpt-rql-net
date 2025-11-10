using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Abstractions.Configuration;

public class RqlSettings : IRqlSettings
{
    public RqlMappingSettings Mapping { get; } = new();

    public RqlSelectSettings Select { get; } = new();

    public RqlFilterSettings Filter { get; } = new();
}

public class GlobalRqlSettings : RqlSettings, IRqlGlobalSettings
{
    public RqlGeneralSettings General { get; } = new();
}
