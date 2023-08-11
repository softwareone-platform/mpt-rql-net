using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Configuration;

namespace Rql.Tests.Unit.Factory;

internal static class RqlSettingsFactory
{
    internal static RqlSettings Default()
    {
        var rqlSettings = new RqlSettings { DefaultActions = RqlAction.Filter };

        return rqlSettings;
    }
}

