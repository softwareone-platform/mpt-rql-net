using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Linq.Services.Graph;

internal interface IGraphBuilder<TView>
{
    void TraverseRqlExpression(RqlNode? target, RqlExpression? expression);
}
