namespace SoftwareOne.Rql.Linq.Core.Configuration
{
    internal class RqlSettings : IRqlSettings
    {
        public RqlSettings()
        {
            IgnoredQueryParameters = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public MemberFlag DefaultMemberFlags { get; set; }
        public HashSet<string> IgnoredQueryParameters { get; init; }
    }
}
