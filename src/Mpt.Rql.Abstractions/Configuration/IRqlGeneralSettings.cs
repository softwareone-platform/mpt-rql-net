namespace Mpt.Rql.Abstractions.Configuration;

/// <summary>
/// General RQL configuration settings
/// </summary>
public interface IRqlGeneralSettings
{
    RqlActions DefaultActions { get; set; }

    RqlOperators AllowedOperators { get; set; }
}
