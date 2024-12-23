namespace SoftwareOne.Rql.Linq.Configuration;

internal class RqlSettingsAccessor(IRqlGlobalSettings globalSettings) : IRqlSettingsAccessor
{
    private IRqlSettings? _override;

    public IRqlSettings Current => _override ?? (RqlSettings)globalSettings;

    public void Override(RqlSettings? settings) => _override = settings;
}
