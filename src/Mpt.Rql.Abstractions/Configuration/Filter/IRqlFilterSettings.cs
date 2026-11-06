namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Configuration for RQL filtering behavior
/// </summary>
public interface IRqlFilterSettings
{
    IRqlStringFilterSettings Strings { get; }

    /// <summary>
    /// Controls the navigation strategy for filtering expressions
    /// </summary>
    NavigationStrategy Navigation { get; set; }

    /// <summary>
    /// Controls how the filter of a mapped query is optimized (not at all by default)
    /// </summary>
    FilterOptimization Optimization { get; set; }
}
