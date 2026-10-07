namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlTransformOptions
{
    IRqlSettings Settings { get; }

    /// <summary>
    /// Sets the visibility of the property at the dotted <paramref name="path"/> for this call. A shown property is
    /// selected regardless of its action strategy and select mode wherever the property above it is in the selection,
    /// unless the request deselects either of them. A hidden property is treated as if its action strategy allowed
    /// nothing: it cannot be filtered or ordered by, and it is not selected unless it is forced. Paths that name no
    /// property are ignored, and the last call on a path wins.
    /// </summary>
    void SetVisibility(string path, RqlVisibility visibility);
}
