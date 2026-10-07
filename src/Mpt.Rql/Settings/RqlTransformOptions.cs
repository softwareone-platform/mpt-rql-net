using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Settings;

internal sealed class RqlTransformOptions(IRqlSettings settings) : IRqlTransformOptions
{
    private Dictionary<string, RqlVisibility>? _decisions;

    public IRqlSettings Settings { get; } = settings;

    /// <summary>The visibility set on each path, or <c>null</c> when none was set.</summary>
    public IReadOnlyDictionary<string, RqlVisibility>? Decisions => _decisions;

    public void SetVisibility(string path, RqlVisibility visibility)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!Enum.IsDefined(visibility))
            throw new ArgumentOutOfRangeException(nameof(visibility));

        (_decisions ??= new(StringComparer.InvariantCultureIgnoreCase))[path] = visibility;
    }
}
