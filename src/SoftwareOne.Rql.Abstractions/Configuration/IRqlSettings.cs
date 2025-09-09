using SoftwareOne.Rql.Abstractions.Configuration.Filter;

namespace SoftwareOne.Rql.Abstractions.Configuration;

public interface IRqlSettings
{
    RqlMappingSettings Mapping { get; init; }

    RqlSelectSettings Select { get; init; }

    RqlFilterSettings Filter { get; init; }
}