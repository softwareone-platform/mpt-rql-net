namespace Mpt.Rql.Abstractions.Configuration;

public interface IRqlGlobalSettings : IRqlSettings
{
    IRqlGeneralSettings General { get; }
}