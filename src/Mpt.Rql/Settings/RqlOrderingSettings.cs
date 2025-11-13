using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Ordering;

namespace Mpt.Rql.Settings;

internal record RqlOrderingSettings : IRqlOrderingSettings
{
    public NavigationStrategy Navigation { get; set; } = NavigationStrategy.Default;
}