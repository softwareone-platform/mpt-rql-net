namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlGeneralSettings
    {
        RqlActions DefaultActions { get; set; }
        RqlOperators AllowedOperators { get; set; }
    }
}
