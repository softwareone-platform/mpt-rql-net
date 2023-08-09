namespace SoftwareOne.Rql.Linq.Services.Projection;

internal class ProjectionNode
{
    public ReadOnlyMemory<char> Value { get; set; }
    public ProjectionNodeType Type { get; set; }
    public bool Sign { get; set; }
    public Dictionary<string, ProjectionNode>? Children { get; set; }
}