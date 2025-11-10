using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.UnitTests.Common.Factory;

internal static class RqlSettingsFactory
{
    internal static GlobalRqlSettings Default()
    {
        var rqlSettings = new GlobalRqlSettings();
        rqlSettings.General.DefaultActions = RqlActions.Filter;
        return rqlSettings;
    }
}

