namespace SoftwareOne.Rql.Linq.Configuration;

internal interface IRqlSettingsAccessor
{
    IRqlSettings Current { get; }

    void Override(RqlSettings? settings);
}
