using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;

namespace Mpt.Rql.Services.Context;

internal class BuilderContext : IBuilderContext, IResettable
{
    public RqlNode? CurrentNode { get; private set; }

    public void SetNode(RqlNode? node)
    {
        CurrentNode = node;
    }

    public bool TryGoToChild(IRqlPropertyInfo rqlProperty)
    {
        if (CurrentNode?.TryGetChild(rqlProperty.Name, out var child) != true)
            return false;

        CurrentNode = child as RqlNode;
        return true;
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

    /// <summary>
    /// Clears navigation state so this instance can be reused within the same scope.
    /// </summary>
    public void Reset()
    {
        CurrentNode = null;
    }
}