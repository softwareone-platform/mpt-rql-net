using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Graph;

namespace Mpt.Rql.Services.Ordering;

internal interface IOrderingGraphBuilder<TView> : IGraphBuilder<TView> { }

internal class OrderingGraphBuilder<TView> : GraphBuilder<TView>, IOrderingGraphBuilder<TView>
{
    public OrderingGraphBuilder(IMetadataProvider metadataProvider, IActionValidator actionValidator, IBuilderContext builderContext)
        : base(metadataProvider, actionValidator, builderContext)
    {
    }

    protected override RqlActions Action => RqlActions.Order;

    protected override RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign)
        => parentNode.IncludeChild(rqlProperty, IncludeReasons.Order);
}
