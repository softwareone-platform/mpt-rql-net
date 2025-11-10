using Mpt.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

internal class AllowOnlyFilterActionStrategy : IActionStrategy
{
    public bool IsAllowed(RqlActions action) => action == RqlActions.Filter;
}
