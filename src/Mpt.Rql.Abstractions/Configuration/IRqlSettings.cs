using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Abstractions.Configuration.Ordering;

namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlSettings
{
    IRqlMappingSettings Mapping { get; }

    IRqlSelectSettings Select { get; }

    IRqlFilterSettings Filter { get; }

    IRqlOrderingSettings Ordering { get; }
}