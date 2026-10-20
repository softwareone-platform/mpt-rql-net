namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Defines where the filter is applied: to the projected view, or to the source before the projection.
/// </summary>
public enum FilterPlacement
{
    /// <summary>
    /// The filter is applied to the projected view. This is the default.
    /// </summary>
    AfterProjection,

    /// <summary>
    /// The filter is applied to the source, before the projection. Null checks that the projection adds around nullable
    /// references are left out where they make no difference, so a filter through nested references reads the source
    /// member directly and the query can use an index on it. Use it with query providers where reading a member of a
    /// missing reference gives null, as database queries do; in-memory queries throw instead.
    /// </summary>
    BeforeProjection
}
