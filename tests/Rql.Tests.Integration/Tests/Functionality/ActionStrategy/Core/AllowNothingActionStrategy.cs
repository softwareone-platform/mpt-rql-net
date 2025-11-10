using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

internal class AllowNothingActionStrategy : IActionStrategy
{
    public bool IsAllowed(RqlActions action) => false;
}
