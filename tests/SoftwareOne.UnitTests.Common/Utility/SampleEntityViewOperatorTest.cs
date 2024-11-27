using SoftwareOne.Rql;

namespace SoftwareOne.UnitTests.Common.Utility;

public class SampleEntityViewOperatorTest : SampleEntityView
{
    [RqlProperty(Operators = RqlOperators.GenericDefaults ^ RqlOperators.Eq ^ RqlOperators.ListIn ^ RqlOperators.ListOut)]
    public override int Id { get; set; }
}