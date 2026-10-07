namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlTransformOptions
{
    IRqlSettings Settings { get; }

    /// <summary>
    /// Sets the visibility of the property at the dotted <paramref name="path"/> for this call. A shown property is
    /// selected regardless of its action strategy and select mode wherever the request or the defaults select the
    /// property above it, unless the request deselects either of them. A hidden property is treated as if its action strategy allowed
    /// nothing: the request can neither select, filter nor order by it, though one declared
    /// <see cref="RqlPropertyMode.Forced"/> is still projected, as with its action strategy. Paths that name no
    /// property are ignored, and the last call on a path wins.
    /// </summary>
    void SetVisibility(string path, RqlVisibility visibility);
}
