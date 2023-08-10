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
}
