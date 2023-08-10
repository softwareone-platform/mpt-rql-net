namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlSettings : IRqlSettings
    {
        public RqlSettings()
        {
            Select = new RqlSelectSettings();
        }

        public MemberFlag DefaultFlags { get; set; }
        public IRqlSelectSettings Select { get; init; }
    }

    internal class RqlSelectSettings : IRqlSelectSettings
    {
        public SelectMode Mode { get; set; }
        public SelectMode ReferenceMode { get; set; }
        public int? MaxDepth { get; set; }
    }
}
