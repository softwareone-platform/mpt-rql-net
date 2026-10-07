using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Settings;

internal sealed class RqlTransformOptions(IRqlSettings settings) : IRqlTransformOptions
{
    private Dictionary<string, bool>? _decisions;

    public IRqlSettings Settings { get; } = settings;

    /// <summary>Whether the property at each path decided on is included, or <c>null</c> when there are no decisions.</summary>
    public IReadOnlyDictionary<string, bool>? Decisions => _decisions;

    public void Override(string path, bool include)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        (_decisions ??= new(StringComparer.InvariantCultureIgnoreCase))[path] = include;
    }
}
