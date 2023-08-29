using SoftwareOne.Rql;

namespace Rql.Tests.Unit.Utility;

public class SampleEntityViewOperatorTest : SampleEntityView
{
    [RqlProperty(Operators = RqlOperators.GenericDefaults ^ RqlOperators.Eq ^ RqlOperators.ListIn ^ RqlOperators.ListOut)]
    public override int Id { get; set; }
}