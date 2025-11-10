using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Linq.Configuration;

namespace Mpt.UnitTests.Common;

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

