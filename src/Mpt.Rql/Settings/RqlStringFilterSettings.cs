using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Settings;

internal record RqlStringFilterSettings : IRqlStringFilterSettings
{
    public StringComparisonStrategy Strategy { get; set; } = StringComparisonStrategy.Default;

    public StringComparison? Comparison { get; set; }
}