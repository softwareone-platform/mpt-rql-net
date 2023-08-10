namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlSelectSettings : IRqlSelectSettings
    {
        public SelectMode Mode { get; set; }
        public SelectMode ReferenceMode { get; set; }
        public int? MaxDepth { get; set; }
    }
}
