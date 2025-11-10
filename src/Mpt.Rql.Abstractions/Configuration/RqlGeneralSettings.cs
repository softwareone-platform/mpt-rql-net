namespace Mpt.Rql.Abstractions.Configuration;

public class RqlGeneralSettings
{
    public RqlActions DefaultActions { get; set; } = RqlActions.All;

    public RqlOperators AllowedOperators { get; set; } = RqlOperators.AllOperators;
}
