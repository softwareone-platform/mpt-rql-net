using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Graph;
using Mpt.Rql.Services.Ordering.Functions;

namespace Mpt.Rql.Services.Ordering;

internal interface IOrderingGraphBuilder<TView> : IGraphBuilder<TView> { }

internal class OrderingGraphBuilder<TView> : GraphBuilder<TView>, IOrderingGraphBuilder<TView>, IOrderingFunctionGraph
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
    /// In an order string every group with a name (after stripping the sign) is a function call: the
    /// function declares the nodes its key reads via <see cref="IOrderingFunction.IncludeInGraph"/>.
    /// Unknown names are claimed too (no graph mutation) so that arguments are never resolved as
    /// root-level properties; the expression stage reports the unknown function. Anonymous and
    /// sign-only groups (<c>+(id,name)</c>) are plain lists of order terms and take the base path.
    /// </summary>
    protected override bool TryTraverseFunctionGroup(RqlNode target, RqlGenericGroup group)
    {
        if (string.IsNullOrEmpty(group.Name))
            return false;

        var (name, _) = StringHelper.ExtractSign(group.Name);
        if (name.Length == 0)
            return false;

        if (_functions.TryGet(name.ToString(), out var function))
            function.IncludeInGraph(this, target, group.Items ?? []);

        return true;
    }

    RqlNode? IOrderingFunctionGraph.IncludeHierarchy(RqlNode target, RqlExpression path)
        => ProcessNode(target, path, hierarchyOnly: true);

    void IOrderingFunctionGraph.TraversePredicate(RqlNode target, RqlExpression predicate)
        => _filteringGraphBuilder.TraverseRqlExpression(target, predicate);

    void IOrderingFunctionGraph.IncludeOrderPath(RqlNode target, RqlExpression path)
        => ProcessNode(target, path);
}
