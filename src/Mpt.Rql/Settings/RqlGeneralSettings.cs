using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Settings;

internal record RqlGeneralSettings : IRqlGeneralSettings
{
    public RqlActions DefaultActions { get; set; } = RqlActions.All;

    public RqlOperators AllowedOperators { get; set; } = RqlOperators.AllOperators;
}