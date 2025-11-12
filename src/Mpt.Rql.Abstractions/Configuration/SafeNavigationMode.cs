namespace Mpt.Rql.Abstractions.Configuration;

/// <summary>
/// Controls whether safe navigation operators (?.) are used in property path expressions
/// </summary>
public enum SafeNavigationMode
{
    /// <summary>
    /// No null conditional operators - use direct property access
    /// </summary>
    Off,
    /// <summary>
    /// Use null conditional operators (?.) for safe property access
    /// </summary>
    On
}