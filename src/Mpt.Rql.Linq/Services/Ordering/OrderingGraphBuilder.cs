using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using Mpt.Rql.Linq.Core.Metadata;
using Mpt.Rql.Linq.Services.Context;
using Mpt.Rql.Linq.Services.Graph;

namespace Mpt.Rql.Linq.Services.Ordering;

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
