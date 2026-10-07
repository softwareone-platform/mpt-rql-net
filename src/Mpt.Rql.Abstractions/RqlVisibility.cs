#pragma warning disable IDE0130
namespace Mpt.Rql;

public enum RqlVisibility
{
    /// <summary>Selected regardless of the property's action strategy and select mode, unless the request deselects it.</summary>
    Shown,

    /// <summary>Treated as if the property's action strategy allowed nothing.</summary>
    Hidden,
}
