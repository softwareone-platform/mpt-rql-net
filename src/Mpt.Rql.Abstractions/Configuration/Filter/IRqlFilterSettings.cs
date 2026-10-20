namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Configuration for RQL filtering behavior
/// </summary>
public interface IRqlFilterSettings
{
    IRqlStringFilterSettings Strings { get; }

    /// <summary>
    /// Controls the navigation strategy for filtering expressions
    /// </summary>
    NavigationStrategy Navigation { get; set; }

    /// <summary>
    /// Controls whether the filter is applied to the projected view (<see cref="FilterBinding.View"/>, the default)
    /// or rewritten onto the storage type and applied before the projection (<see cref="FilterBinding.Storage"/>),
    /// which lets relational providers use indexes for filters through nullable nested references.
    /// </summary>
    FilterBinding Binding { get; set; }
}
