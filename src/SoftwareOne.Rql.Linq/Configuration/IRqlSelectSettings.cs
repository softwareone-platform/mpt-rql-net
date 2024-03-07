namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSelectSettings
    {
        RqlSelectMode Mode { get; set; }
        int? MaxDepth { get; set; }
    }
}
