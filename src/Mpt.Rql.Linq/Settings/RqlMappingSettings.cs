using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Linq.Settings;

internal record RqlMappingSettings : IRqlMappingSettings
{
    public bool Transparent { get; set; }
}