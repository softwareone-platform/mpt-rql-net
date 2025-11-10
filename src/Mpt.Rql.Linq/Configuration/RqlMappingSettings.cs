using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Linq.Configuration;

internal record RqlMappingSettings : IRqlMappingSettings
{
    public bool Transparent { get; set; }
}