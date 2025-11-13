namespace Mpt.Rql.Abstractions.Configuration;

/// <summary>
/// Controls whether safe navigation operators (?.) are used in property path expressions
/// </summary>
public enum SafeNavigationMode
{
    /// <summary>
    /// No null conditional operators - use direct property access. 
    /// In most cases, underlying data source should handle null values appropriately.
    /// </summary>
    Off,
    /// <summary>
    /// Use null conditional operators (?.) for safe property access.
    /// Enabling this setting helps prevent null reference exceptions when accessing nested properties that may be null.
    /// However, it adds performance overhead due to the additional null checks.
    /// </summary>
    On
}