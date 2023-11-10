namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlSelectSettings : IRqlSelectSettings
    {
        public RqlSelectMode Mode { get; set; }
        public int? MaxDepth { get; set; }
    }
}
