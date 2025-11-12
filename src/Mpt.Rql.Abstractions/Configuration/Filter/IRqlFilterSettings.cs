using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Configuration for RQL filtering behavior
/// </summary>
public interface IRqlFilterSettings
{
    IRqlStringFilterSettings Strings { get; }
    
    /// <summary>
    /// Controls whether safe navigation operators (?.) are used in filtering expressions
    /// </summary>
    SafeNavigationMode SafeNavigation { get; set; }
}
