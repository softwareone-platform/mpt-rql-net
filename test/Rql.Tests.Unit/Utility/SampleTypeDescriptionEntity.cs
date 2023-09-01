using SoftwareOne.Rql;

namespace Rql.Tests.Unit.Utility;

internal class SampleTypeDescriptionEntity
{
    [RqlProperty(RqlActions.Order)]
    public string OrderProp { get; set; } = null!;
    [RqlProperty(RqlActions.Filter)]
    public int FilterProp { get; set; }
    [RqlProperty(RqlActions.All)]
    public string AllProp { get; set; } = null!;
    [RqlProperty(RqlActions.Select)]
    public string SelectProp { get; set; } = null!;
    [RqlProperty(RqlActions.None)]
    public string NoneProp { get; set; } = null!;
}
