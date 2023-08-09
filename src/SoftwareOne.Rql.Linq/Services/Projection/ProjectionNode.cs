namespace SoftwareOne.Rql.Linq.Services.Projection
{
    internal class ProjectionNode
    {
        public ReadOnlyMemory<char> Value { get; set; }
        public NodeMode Mode { get; set; }
        public Dictionary<string, ProjectionNode>? Children { get; set; }

        public bool TryGetChild(string name, out ProjectionNode? child)
        {
            child = null;
            if (Children == null)
                return false;

            return Children.TryGetValue(name, out child);
        }

        public enum NodeMode
        {
            Default,
            Add,
            Subtract
        }
    }
}
