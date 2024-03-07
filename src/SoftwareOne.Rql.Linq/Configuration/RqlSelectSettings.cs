namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlSelectSettings : IRqlSelectSettings
    {
        public RqlSelectModes Mode { get; set; }
        public int? MaxDepth { get; set; }
    }
}
