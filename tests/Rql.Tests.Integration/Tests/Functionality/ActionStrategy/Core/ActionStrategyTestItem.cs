using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;

public class ActionStrategyTestItem : ITestEntity
{
    public int Id { get; set; }

    [RqlProperty(ActionStrategy = typeof(AllowAllActionStrategy))]
    public Link? All { get; set; }

    [RqlProperty(ActionStrategy = typeof(AllowNothingActionStrategy))]
    public Link? Nothing { get; set; }

    [RqlProperty(ActionStrategy = typeof(AllowOnlyFilterActionStrategy))]
    public Link? FilterOnly { get; set; }

    [RqlProperty(ActionStrategy = typeof(AllowOnlyOrderActionStrategy))]
    public Link? OrderOnly { get; set; }

    [RqlProperty(ActionStrategy = typeof(AllowOnlySelectActionStrategy))]
    public Link? SelectOnly { get; set; }

    public class Link
    {
        [RqlProperty(IsCore = true)]
        public string Foo { get; set; } = "bar";
    }
}


