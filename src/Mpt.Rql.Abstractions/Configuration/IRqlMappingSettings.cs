namespace Mpt.Rql.Abstractions.Configuration;

/// <summary>
/// Configuration for RQL mapping behavior
/// </summary>
public interface IRqlMappingSettings
{
    /// <summary>
    /// When set to true RQL does not try to map source type to view type if they are the same.
    /// Note that enabling this feature may increase amount of data transferred between application and database server.
    /// </summary>
    bool Transparent { get; set; }

    /// <summary>
    /// Controls the navigation strategy for mapping expressions.
    /// When set to <see cref="NavigationStrategy.Safe"/>, uses null conditional operators (?.) for safe property access during mapping.
    /// This helps prevent null reference exceptions when accessing nested properties that may be null.
    /// </summary>
    NavigationStrategy NullPropagation { get; set; }
}
