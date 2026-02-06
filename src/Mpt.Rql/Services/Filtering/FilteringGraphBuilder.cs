using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Graph;

namespace Mpt.Rql.Services.Filtering;

internal interface IFilteringGraphBuilder<TView> : IGraphBuilder<TView> { }

internal class FilteringGraphBuilder<TView> : GraphBuilder<TView>, IFilteringGraphBuilder<TView>
{
    public FilteringGraphBuilder(IMetadataProvider metadataProvider, IActionValidator actionValidator, IBuilderContext builderContext)
        : base(metadataProvider, actionValidator, builderContext)
    {
    }

    protected override RqlActions Action => RqlActions.Filter;

    protected override RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign)
        => parentNode.IncludeChild(rqlProperty, IncludeReasons.Filter);
}
