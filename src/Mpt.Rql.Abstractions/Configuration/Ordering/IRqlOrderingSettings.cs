using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Abstractions.Configuration.Ordering;

/// <summary>
/// Configuration for RQL ordering behavior
/// </summary>
public interface IRqlOrderingSettings
{
    /// <summary>
    /// Controls whether safe navigation operators (?.) are used in ordering expressions
    /// </summary>
    SafeNavigationMode SafeNavigation { get; set; }
}