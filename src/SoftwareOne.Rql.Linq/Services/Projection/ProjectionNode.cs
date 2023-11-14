namespace SoftwareOne.Rql.Linq.Services.Projection;
internal partial class ProjectionNode
{
    public ProjectionNode? Parent { get; set; }
    public ReadOnlyMemory<char> Value { get; set; }
    public RqlSelectMode Mode { get; set; }
    public Dictionary<string, ProjectionNode>? Children { get; set; }

    internal bool TryGetChild(string name, out ProjectionNode? child)
    {
        child = null;
        if (Children == null)
            return false;

        return Children.TryGetValue(name, out child);
    }

    internal string GetFullPath()
    {
        string? path = null;
        
        if (Parent != null)
            path = Parent.GetFullPath();

        return string.IsNullOrEmpty(path) ? Value.ToString() : $"{path}.{Value}";
    }
}
