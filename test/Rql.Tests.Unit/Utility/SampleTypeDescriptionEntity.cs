using SoftwareOne.Rql;

namespace Rql.Tests.Unit.Utility;

internal class SampleTypeDescriptionEntity
{
    [RqlProperty(RqlAction.Order)]
    public string OrderProp { get; set; }
    [RqlProperty(RqlAction.Filter)]
    public int FilterProp { get; set; }
    [RqlProperty(RqlAction.All)]
    public string AllProp { get; set; }
    [RqlProperty(RqlAction.Select)]
    public string SelectProp { get; set; }
    [RqlProperty(RqlAction.None)]
    public string NoneProp { get; set; }
}
