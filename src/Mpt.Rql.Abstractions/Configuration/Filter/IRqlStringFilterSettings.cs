namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Configuration for string filtering behavior
/// </summary>
public interface IRqlStringFilterSettings
{
    StringComparisonType Type { get; set; }
}

public enum StringComparisonType
{
    Simple,
    Lexicographical
}
