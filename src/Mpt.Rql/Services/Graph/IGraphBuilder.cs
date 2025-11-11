using Mpt.Rql.Abstractions;

namespace Mpt.Rql.Services.Graph;

internal interface IGraphBuilder<TView>
{
    void TraverseRqlExpression(RqlNode? target, RqlExpression? expression);
}
