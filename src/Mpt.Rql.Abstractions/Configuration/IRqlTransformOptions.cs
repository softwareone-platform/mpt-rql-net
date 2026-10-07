namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlTransformOptions
{
    IRqlSettings Settings { get; }

    /// <summary>
    /// Overrides the property at the dotted <paramref name="path"/> in this call. Included, it is selected regardless of
    /// its action strategy and select mode, unless the request deselects it. Excluded, it is treated as if its action
    /// strategy allowed nothing, so it cannot be selected, filtered or ordered by.
    /// </summary>
    void Override(string path, bool include);
}
