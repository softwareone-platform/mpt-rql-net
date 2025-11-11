using Mpt.Rql.Abstractions.Configuration.Filter;

namespace Mpt.Rql.Settings;

internal record RqlStringFilterSettings : IRqlStringFilterSettings
{
    public StringComparisonType Type { get; set; } = StringComparisonType.Simple;
}