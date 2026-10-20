using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Builders;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering;

internal delegate BinaryExpression LogicalExpression(Expression left, Expression right);

internal sealed class FilteringService<TView> : RqlService, IFilteringService<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IFilteringGraphBuilder<TView> _graphBuilder;
    private readonly IExpressionBuilder _builder;
    private readonly IRqlParser _parser;
    private readonly IRqlSettings _settings;

    public FilteringService(IQueryContext<TView> context, IFilteringGraphBuilder<TView> graphBuilder, IExpressionBuilder builder, IRqlParser parser, IRqlSettings settings)
    {
        _context = context;
        _graphBuilder = graphBuilder;
        _builder = builder;
        _parser = parser;
        _settings = settings;
    }

    protected override string ErrorPrefix => "query";

    public void Process(string? filter)
    {
        if (string.IsNullOrEmpty(filter))
            return;

        if (!TryParse(_parser, filter, out var parseResult, out var parseError))
        {
            _context.AddError(parseError!);
            return;
        }

        RqlExpression rql;
        if (parseResult.Items?.Count == 1 && parseResult is RqlGenericGroup genGrp && genGrp.Name == string.Empty)
            rql = parseResult.Items[0];
        else
            rql = parseResult;

        _graphBuilder.TraverseRqlExpression(_context.Graph, rql);

        var param = Expression.Parameter(typeof(TView));
        var expression = _builder.Build(param, rql);

        if (expression.IsError)
        {
            _context.AddErrors(expression.Errors);
            return;
        }

        _context.AddTransformation(q =>
        {
            var lambda = (Expression<Func<TView, bool>>)Expression.Lambda(expression.Value!, param);
            return _settings.Filter.Placement == FilterPlacement.BeforeProjection
                ? FilterPushdownVisitor.Apply(q, lambda)
                : q.Where(lambda);
        });
    }
}