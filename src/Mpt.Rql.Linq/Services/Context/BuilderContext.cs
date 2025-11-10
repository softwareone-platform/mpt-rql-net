using Mpt.Rql.Abstractions;

namespace Mpt.Rql.Linq.Services.Context;

internal class BuilderContext : IBuilderContext
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
}