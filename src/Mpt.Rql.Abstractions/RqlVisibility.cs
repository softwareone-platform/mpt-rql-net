#pragma warning disable IDE0130
namespace Mpt.Rql;

public enum RqlVisibility
{
    /// <summary>
    /// Selected regardless of the property's action strategy and select mode wherever RQL builds the defaults of the
    /// property above it, unless the request deselects either of them.
    /// </summary>
    Shown,

    /// <summary>Treated as if the property's action strategy allowed nothing.</summary>
    Hidden,
}
