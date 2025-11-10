namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Configuration for RQL filtering behavior
/// </summary>
public interface IRqlFilterSettings
{
    IRqlStringFilterSettings Strings { get; }
}
