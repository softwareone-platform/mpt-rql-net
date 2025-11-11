using Mpt.Rql;
using Mpt.Rql.Abstractions;

namespace Rql.Tests.Unit.Services;

internal class GraphPrinter
{
    private readonly Dictionary<string, string> _properties;

    internal GraphPrinter()
    {
        _properties = [];
    }

    public void Graph(IRqlNode node)
    {
        foreach (var child in node.Children.OrderBy(t => t.Name))
        {
            Property(child.GetFullPath(), child.IncludeReason, child.ExcludeReason);
            Graph(child);
        }
    }

    public void Property(string path, IncludeReasons includeReasons, ExcludeReasons excludeReasons)
    {
        var value = $"{path}:{includeReasons}:{excludeReasons}";

        if (!_properties.TryAdd(path, value))
        {
            _properties[path] = value;
        }
    }

    public void Remove(string path)
    {
        _properties.Remove(path);
    }

    public IEnumerable<string> Properties => _properties.Values;
}
