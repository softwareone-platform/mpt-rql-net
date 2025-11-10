using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Linq.Configuration;

internal class RqlSettingsAccessor(IRqlGlobalSettings globalSettings) : IRqlSettingsAccessor
{
    private RqlSettings? _instance;

    public IRqlSettings Current => GetInstance();

    internal RqlSettings GetInstance()
    {
        if (_instance != null)
            return _instance;

        _instance = new RqlSettings();
        // deep clone from global settings
        var globalAsSettings = (RqlSettings)globalSettings;
        
        // Deep copy Mapping settings
        _instance.Mapping.Transparent = globalAsSettings.Mapping.Transparent;
        
        // Deep copy Select settings  
        _instance.Select.Implicit = globalAsSettings.Select.Implicit;
        _instance.Select.Explicit = globalAsSettings.Select.Explicit;
        _instance.Select.MaxDepth = globalAsSettings.Select.MaxDepth;
        
        // Deep copy Filter settings
        _instance.Filter.Strings.Type = globalAsSettings.Filter.Strings.Type;
        return _instance;
    }
}
