using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core
{
    internal class AllowAllActionStrategy : IActionStrategy
    {
        public bool IsAllowed(RqlActions action) => true;
    }
}
