namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Defines how RQL optimizes the filter of a mapped query.
/// </summary>
public enum FilterOptimization
{
    /// <summary>
    /// The filter is applied to the projected view as it is. This is the default.
    /// </summary>
    None,

    /// <summary>
    /// The filter is applied to the source, before the projection, and the null checks that the projection adds around
    /// nullable references are removed where they make no difference. A filter through nested references then reads the
    /// source member directly, and the query can use an index on it. Use it with query providers where reading a member
    /// of a missing reference gives null, as database queries do; in-memory queries throw instead. It applies with the
    /// default navigation only: with safe filter or mapping navigation every member read gets its own null check, so the
    /// filter stays on the projected view, as with <see cref="None"/>.
    /// </summary>
    RemoveRedundantNullChecks
}
