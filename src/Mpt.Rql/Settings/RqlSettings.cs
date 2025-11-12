using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Abstractions.Configuration.Ordering;

namespace Mpt.Rql.Settings;

internal record RqlSettings : IRqlSettings
{
    public IRqlMappingSettings Mapping { get; } = new RqlMappingSettings();

    public IRqlSelectSettings Select { get; } = new RqlSelectSettings();

    public IRqlFilterSettings Filter { get; } = new RqlFilterSettings();

    public IRqlOrderingSettings Ordering { get; } = new RqlOrderingSettings();
}

internal record GlobalRqlSettings : RqlSettings, IRqlGlobalSettings
{
    public IRqlGeneralSettings General { get; } = new RqlGeneralSettings();
}