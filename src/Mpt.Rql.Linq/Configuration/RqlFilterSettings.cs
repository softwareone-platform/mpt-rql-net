using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Linq.Configuration;

internal record RqlFilterSettings : IRqlFilterSettings
{
    public IRqlStringFilterSettings Strings { get; init; } = new RqlStringFilterSettings();
}