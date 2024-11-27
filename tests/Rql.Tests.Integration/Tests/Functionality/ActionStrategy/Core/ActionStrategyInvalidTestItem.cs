using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core
{
    public class ActionStrategyInvalidTestItem : ITestEntity
    {
        public int Id { get; set; }

        [RqlProperty(ActionStrategy = typeof(object))]
        public int InvalidStrategy { get; set; }
    }
}