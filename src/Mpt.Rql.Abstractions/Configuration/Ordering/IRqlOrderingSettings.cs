using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Abstractions.Configuration.Ordering;

/// <summary>
/// Configuration for RQL ordering behavior
/// </summary>
public interface IRqlOrderingSettings
{
    /// <summary>
    /// Controls the navigation strategy for ordering expressions
    /// </summary>
    NavigationStrategy Navigation { get; set; }
}