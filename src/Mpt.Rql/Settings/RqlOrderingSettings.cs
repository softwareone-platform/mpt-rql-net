using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Ordering;

namespace Mpt.Rql.Settings;

internal record RqlOrderingSettings : IRqlOrderingSettings
{
    public SafeNavigationMode SafeNavigation { get; set; } = SafeNavigationMode.Off;
}