using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Settings;

internal sealed class RqlTransformOptions(IRqlSettings settings) : IRqlTransformOptions
{
    private readonly List<Action<IRqlNode>> _graphCallbacks = [];

    public IRqlSettings Settings { get; } = settings;

    public IReadOnlyList<Action<IRqlNode>> GraphCallbacks => _graphCallbacks;

    public void OnGraphBuilt(Action<IRqlNode> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _graphCallbacks.Add(callback);
    }
}
