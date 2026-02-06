
#pragma warning disable IDE0130
namespace Mpt.Rql;
/// <summary>
/// Defines the visibility and selection behavior of a property in RQL queries.
/// </summary>
public enum RqlPropertyMode
{
    /// <summary>
    /// Default mode. Property follows standard RQL selection rules based on:
    /// - IsCore flag
    /// - Select settings (Implicit/Explicit modes)
    /// - MaxDepth settings
    /// - Explicit inclusion/exclusion in select clause
    /// </summary>
    Default,

    /// <summary>
    /// Property is excluded from all RQL operations (select, filter, order).
    /// It will never appear in query results or be available for filtering/ordering,
    /// regardless of other settings or explicit selection attempts.q
    /// Use for sensitive data or internal-only properties.
    /// </summary>
    Ignored,

    /// <summary>
    /// Property is always included in query results, bypassing normal selection rules.
    /// It ignores:
    /// - MaxDepth restrictions
    /// - Implicit/Explicit select mode settings
    /// - Explicit exclusion attempts (e.g., -propertyName)
    /// Use for properties that must always be present (e.g., identifiers, required fields).
    /// </summary>
    Forced
}