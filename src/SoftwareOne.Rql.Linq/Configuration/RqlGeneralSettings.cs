namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlGeneralSettings : IRqlGeneralSettings
    {
        public RqlGeneralSettings()
        {
            AllowedOperators = RqlOperators.AllOperators;
        }

        public RqlActions DefaultActions { get; set; }
        public RqlOperators AllowedOperators { get; set; }
    }
}
