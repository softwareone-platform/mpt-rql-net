using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

internal class AllowOnlySelectActionStrategy : IActionStrategy
{
    public bool IsAllowed(RqlActions action) => action == RqlActions.Select;
}
