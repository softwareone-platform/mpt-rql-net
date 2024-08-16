namespace SoftwareOne.Rql.Linq.Configuration
{
    public class RqlGeneralSettings
    {
        public RqlActions DefaultActions { get; set; }

        public RqlOperators AllowedOperators { get; set; } = RqlOperators.AllOperators;
    }
}
