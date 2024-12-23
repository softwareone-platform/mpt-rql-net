using SoftwareOne.Rql.Linq.Configuration.Filter;

namespace SoftwareOne.Rql.Linq.Configuration;

internal interface IRqlSettings
{
    RqlMappingSettings Mapping { get; init; }

    RqlSelectSettings Select { get; init; }

    RqlFilterSettings Filter { get; init; }
}