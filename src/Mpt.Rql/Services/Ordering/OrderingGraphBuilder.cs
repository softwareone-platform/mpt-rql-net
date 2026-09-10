using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Graph;
using Mpt.Rql.Services.Ordering.Functions;

namespace Mpt.Rql.Services.Ordering;

internal interface IOrderingGraphBuilder<TView> : IGraphBuilder<TView> { }

internal class OrderingGraphBuilder<TView> : GraphBuilder<TView>, IOrderingGraphBuilder<TView>
{
    private readonly IFilteringGraphBuilder<TView> _filteringGraphBuilder;
    private readonly OrderingFunctionRegistry _functions;

    public OrderingGraphBuilder(
        IMetadataProvider metadataProvider,
        IActionValidator actionValidator,
        IBuilderContext builderContext,
        IFilteringGraphBuilder<TView> filteringGraphBuilder,
        OrderingFunctionRegistry functions)
        : base(metadataProvider, actionValidator, builderContext)
    {
        _filteringGraphBuilder = filteringGraphBuilder;
        _functions = functions;
    }

    protected override RqlActions Action => RqlActions.Order;

    protected override RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign)
        => parentNode.IncludeChild(rqlProperty, IncludeReasons.Order);

    /// <summary>
    /// In an order string every named group is a function call. Registered functions have the
    /// shape <c>name(collection, [predicate,] path)</c>: the collection path is included as
    /// hierarchy, the predicate is traversed by the filtering builder under the collection node
    /// (exactly like <c>any()</c>), and the selector is included under it with the Order reason.
    /// Unknown names are claimed too (no graph mutation) so that arguments are never resolved as
    /// root-level properties; the expression stage reports the unknown function.
    /// </summary>
    protected override bool TryTraverseFunctionGroup(RqlNode target, RqlGenericGroup group)
    {
        if (group.Name.Length == 0)
            return false;

        var (name, _) = StringHelper.ExtractSign(group.Name);
        if (!_functions.Contains(name.ToString()))
            return true;

        var args = group.Items ?? [];
        if (args.Count is not (2 or 3))
            return true;

        var collectionNode = ProcessNode(target, args[0], hierarchyOnly: true);
        if (collectionNode is null)
            return true;

        if (args.Count == 3)
            _filteringGraphBuilder.TraverseRqlExpression(collectionNode, args[1]);

        ProcessNode(collectionNode, args[^1]);
        return true;
    }
}
