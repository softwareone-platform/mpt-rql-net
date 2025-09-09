using SoftwareOne.Rql;
using SoftwareOne.Rql.Abstractions.Configuration;
using SoftwareOne.Rql.Linq.Configuration;

namespace SoftwareOne.UnitTests.Common;

internal static class RqlSettingsFactory
{
    internal static GlobalRqlSettings Default()
    {
        var rqlSettings = new GlobalRqlSettings
        {
            General = new RqlGeneralSettings { DefaultActions = RqlActions.Filter }
        };

        return rqlSettings;
    }
}

