using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Settings;

internal record RqlFilterSettings : IRqlFilterSettings
{
    public IRqlStringFilterSettings Strings { get; init; } = new RqlStringFilterSettings();
    
    public SafeNavigationMode SafeNavigation { get; set; } = SafeNavigationMode.Off;
}