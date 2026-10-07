using Mpt.Rql.Abstractions;

namespace Mpt.Rql.Services.Context;

internal class BuilderContext : IBuilderContext
{
    public RqlNode? CurrentNode { get; private set; }

    public void SetNode(RqlNode? node)
    {
        CurrentNode = node;
    }

    public bool TryGoToChild(IRqlPropertyInfo rqlProperty) => TryGoToChild(rqlProperty.Name);

    public bool TryGoToChild(string name)
    {
        if (CurrentNode?.TryGetChild(name, out var child) != true)
            return false;

        CurrentNode = child as RqlNode;
        return true;
    }

    /// <summary>
    /// Walks down the path segment by segment (graph node names are matched case-insensitively, like metadata). The
    /// graph stage has already created these nodes; if a step is missing we stop and later error paths simply lack the
    /// prefix.
    /// </summary>
    public void DescendInto(string path)
    {
        foreach (var segment in path.Split('.'))
        {
            if (!TryGoToChild(segment))
                return;
        }
    }

    public void GoToRoot()
    {
        while (CurrentNode?.Parent is not null)
            CurrentNode = CurrentNode.Parent as RqlNode;
    }

    public string GetFullPath(string suffix)
    {
        var result = CurrentNode?.GetFullPath() ?? string.Empty;
        if (!string.IsNullOrEmpty(result))
            result += ".";
        return result + suffix;
    }
}
