namespace SoftwareOne.Rql.Abstractions;

public interface IRqlNode
{
    IEnumerable<IRqlNode> Children { get; }
    int Count { get; }
    int Depth { get; init; }
    ExcludeReasons ExcludeReason { get; }
    IncludeReasons IncludeReason { get; }
    bool IsIncluded { get; }
    string Name { get; init; }
    IRqlNode? Parent { get; }
    IRqlPropertyInfo Property { get; }
    string Print();
    bool TryGetChild(string name, out IRqlNode? child);
    string GetFullPath();
}


[Flags]
public enum IncludeReasons
{
    None = 0,
    Default = 1 << 0,
    Select = 1 << 1,
    Hierarchy = 1 << 2,
    Filter = 1 << 3,
    Order = 1 << 4
}

[Flags]
public enum ExcludeReasons
{
    None = 0,
    Default = 1 << 0,
    Unselected = 1 << 1,
    Invisible = 1 << 2,
}