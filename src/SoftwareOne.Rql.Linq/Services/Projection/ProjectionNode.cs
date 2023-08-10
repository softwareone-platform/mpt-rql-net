namespace SoftwareOne.Rql.Linq.Services.Projection
{
    internal partial class ProjectionNode
    {
        public ReadOnlyMemory<char> Value { get; set; }
        public SelectMode Mode { get; set; }
        public Dictionary<string, ProjectionNode>? Children { get; set; }

        public bool TryGetChild(string name, out ProjectionNode? child)
        {
            child = null;
            if (Children == null)
                return false;

            return Children.TryGetValue(name, out child);
        }
    }
}
