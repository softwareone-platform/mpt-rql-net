using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Settings;

internal record RqlMappingSettings : IRqlMappingSettings
{
    public bool Transparent { get; set; }
    public NavigationStrategy NullPropagation { get; set; }
}