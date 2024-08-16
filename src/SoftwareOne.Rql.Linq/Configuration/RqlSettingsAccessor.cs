namespace SoftwareOne.Rql.Linq.Configuration;

internal class RqlSettingsAccessor(IRqlGlobalSettings globalSettings) : IRqlSettingsAccessor
{
    private IRqlSettings? _override;

    public IRqlGlobalSettings Global { get; init; } = globalSettings;

    public IRqlSettings Current => _override ?? Global;

    public void Override(RqlSettings? settings) => _override = settings;
}
