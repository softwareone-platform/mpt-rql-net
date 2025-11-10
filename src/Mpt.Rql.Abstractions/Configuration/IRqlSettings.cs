using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlSettings
{
    IRqlMappingSettings Mapping { get; }

    IRqlSelectSettings Select { get; }

    IRqlFilterSettings Filter { get; }
}