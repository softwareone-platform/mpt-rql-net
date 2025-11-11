namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Configuration for string filtering behavior
/// </summary>
public interface IRqlStringFilterSettings
{
    /// <summary>
    /// Strategy for string comparison operations in filtering
    /// </summary>
    StringComparisonStrategy Strategy { get; set; }

    /// <summary>
    /// String comparison mode for case sensitivity. When null, uses default provider behavior.
    /// When specified, overrides the default comparison with the given StringComparison.
    /// </summary>
    StringComparison? Comparison { get; set; }
}

public enum StringComparisonStrategy
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
