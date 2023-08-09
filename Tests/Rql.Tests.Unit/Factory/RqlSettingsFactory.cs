using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Core.Configuration;

namespace Rql.Tests.Unit.Factory;

internal static class RqlSettingsFactory
{
    internal static RqlSettings Default()
    {
        var rqlSettings =  new RqlSettings { DefaultMemberFlags = MemberFlag.AllowFilter };

        return rqlSettings;
    }
}

