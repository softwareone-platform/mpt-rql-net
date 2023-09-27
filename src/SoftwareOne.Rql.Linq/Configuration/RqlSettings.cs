namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlSettings : IRqlSettings
    {
        public RqlSettings()
        {
            Select = new RqlSelectSettings();
            AllowedOperators = RqlOperators.AllOperators;
        }

        public RqlActions DefaultActions { get; set; }
        public RqlOperators AllowedOperators { get; set; }
        public IRqlSelectSettings Select { get; init; }
    }
}
