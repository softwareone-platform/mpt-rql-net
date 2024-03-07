namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSelectSettings
    {
        RqlSelectModes Mode { get; set; }
        int? MaxDepth { get; set; }
    }
}
