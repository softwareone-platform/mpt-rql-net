namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlSettings : IRqlSettings
    {
        public RqlSettings()
        {
            Select = new RqlSelectSettings();
        }

        public MemberFlag DefaultMemberFlags { get; set; }
        public IRqlSelectSettings Select { get; init; }
    }

    internal class RqlSelectSettings : IRqlSelectSettings
    {
        public SelectMode ObjectMode { get; set; }
        public SelectMode ReferenceMode { get; set; }
        public int? MaxSelectDepth { get; set; }
    }
}
