namespace SoftwareOne.Rql.Linq.Configuration;

internal interface IRqlGlobalSettings : IRqlSettings
{
    RqlGeneralSettings General { get; }
}