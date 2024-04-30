namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSelectSettings
    {
        RqlSelectModes Implicit { get; set; }

        RqlSelectModes Explicit { get; set; }

        int? MaxDepth { get; set; }
    }
}
