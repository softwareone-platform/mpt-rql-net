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
    /// Controls whether the filter is applied to the projected view (the default) or to the source, before the projection
    /// </summary>
    FilterPlacement Placement { get; set; }
}
