using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Configuration;

namespace Rql.Tests.Unit.Factory;

internal static class RqlSettingsFactory
{
    internal static RqlGeneralSettings Default()
    {
        var rqlSettings = new RqlGeneralSettings { DefaultActions = RqlActions.Filter };

        return rqlSettings;
    }
}

