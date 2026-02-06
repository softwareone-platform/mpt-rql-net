using Mpt.Rql.Abstractions;
using Mpt.Rql.Services.Context;

namespace Mpt.Rql.Services.Projection;

internal sealed class ProjectionService<TView> : RqlService, IProjectionService<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IRqlParser _parser;
    private readonly IProjectionGraphBuilder<TView> _graphBuilder;

    public ProjectionService(IQueryContext<TView> context, IRqlParser parser, IProjectionGraphBuilder<TView> graphBuilder)
    {
        _context = context;
        _parser = parser;
        _graphBuilder = graphBuilder;
    }

    protected override string ErrorPrefix => "select";

    public void Process(string? projection)
    {
        if (string.IsNullOrEmpty(projection))
            return;

        _graphBuilder.TraverseRqlExpression(_context.Graph, _parser.Parse(projection));
    }
}
