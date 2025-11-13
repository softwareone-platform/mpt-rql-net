namespace Mpt.Rql.Abstractions.Configuration;

/// <summary>
/// Defines strategies for navigating object properties. 
/// </summary>
public enum NavigationStrategy
{
    /// <summary>
    /// Default behavior - underlying data provider decides whether to use safe navigation or not.
    /// </summary>
    Default,
    /// <summary>
    /// Use null conditional operators (?.) for safe property access.
    /// Enabling this setting helps prevent null reference exceptions when accessing nested properties that may be null.
    /// However, it adds performance overhead due to the additional null checks.
    /// </summary>
    Safe
}