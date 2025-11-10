using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlSettings
{
    RqlMappingSettings Mapping { get; }

    RqlSelectSettings Select { get; }

    RqlFilterSettings Filter { get; }
}