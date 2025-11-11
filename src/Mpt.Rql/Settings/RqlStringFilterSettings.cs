using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Settings;

internal record RqlStringFilterSettings : IRqlStringFilterSettings
{
    public StringComparisonType ComparisonType { get; set; } = StringComparisonType.Simple;

    public bool CaseInsensitive { get; set; }
}