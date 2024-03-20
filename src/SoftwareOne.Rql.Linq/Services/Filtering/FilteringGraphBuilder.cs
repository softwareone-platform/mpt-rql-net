using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Graph;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal interface IFilteringGraphBuilder<TView> : IGraphBuilder<TView> { }

internal class FilteringGraphBuilder<TView> : GraphBuilder<TView>, IFilteringGraphBuilder<TView>
{
    public FilteringGraphBuilder(IMetadataProvider metadataProvider, IActionValidator actionValidator)
        : base(metadataProvider, actionValidator)
    {
    }

    protected override RqlActions Action => RqlActions.Filter;

    protected override RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign)
        => parentNode.IncludeChild(rqlProperty, IncludeReasons.Filter);
}
