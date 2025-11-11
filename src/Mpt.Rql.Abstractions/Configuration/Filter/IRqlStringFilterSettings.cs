namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Configuration for string filtering behavior
/// </summary>
public interface IRqlStringFilterSettings
{
    /// <summary>
    /// Type of string comparison to be used in filtering operations 
    /// </summary>
    StringComparisonType ComparisonType { get; set; }

    /// <summary>
    /// If true, all string comparisons will be case insensitive. Otherwise, case sensitivity will depend on the underlying data source.
    /// </summary>
    bool CaseInsensitive { get; set; }
}

public enum StringComparisonType
{
    /// <summary>
    /// Simple comparison using standard operators 
    /// </summary>
    Simple,
    /// <summary>
    /// Lexicographical comparison using string.Compare 
    /// </summary>
    Lexicographical
}
