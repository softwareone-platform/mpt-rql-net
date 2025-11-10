using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Linq.Settings;

internal class RqlSettingsAccessor(IRqlGlobalSettings globalSettings) : IRqlSettingsAccessor
{
    private IRqlSettings? _instance;

    public IRqlSettings Current => GetInstance();

    internal IRqlSettings GetInstance()
    {
        if (_instance != null)
            return _instance;

        // Create a deep copy of the global settings as a session-specific RqlSettings instance
        _instance = new RqlSettings();

        // Deep copy Mapping settings
        _instance.Mapping.Transparent = globalSettings.Mapping.Transparent;

        // Deep copy Select settings  
        _instance.Select.Implicit = globalSettings.Select.Implicit;
        _instance.Select.Explicit = globalSettings.Select.Explicit;
        _instance.Select.MaxDepth = globalSettings.Select.MaxDepth;

        // Deep copy Filter settings
        _instance.Filter.Strings.Type = globalSettings.Filter.Strings.Type;

        return _instance;
    }
}
