using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlSettings
{
    RqlMappingSettings Mapping { get; init; }

    RqlSelectSettings Select { get; init; }

    RqlFilterSettings Filter { get; init; }
}