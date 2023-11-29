using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core
{
    internal class AllowOnlyOrderActionStrategy : IActionStrategy
    {
        public bool IsAllowed(RqlActions action) => action == RqlActions.Order;
    }
}
