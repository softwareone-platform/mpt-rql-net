namespace Rql.Tests.Performance.Model;

public class SampleEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public long Date { get; set; }
}