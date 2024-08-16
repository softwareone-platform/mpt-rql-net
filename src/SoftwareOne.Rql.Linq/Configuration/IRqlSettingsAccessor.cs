namespace SoftwareOne.Rql.Linq.Configuration;

internal interface IRqlSettingsAccessor
{
    IRqlSettings Current { get; }

    IRqlGlobalSettings Global { get; }

    void Override(RqlSettings? settings);
}
