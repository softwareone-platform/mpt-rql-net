using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Filtering.Builders;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal delegate BinaryExpression LogicalExpression(Expression left, Expression right);

internal sealed class FilteringService<TView> : RqlService, IFilteringService<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IFilteringGraphBuilder<TView> _graphBuilder;
    private readonly IExpressionBuilder _builder;
    private readonly IRqlParser _parser;

    public FilteringService(IQueryContext<TView> context, IFilteringGraphBuilder<TView> graphBuilder, IExpressionBuilder builder, IRqlParser parser)
    {
        _context = context;
        _graphBuilder = graphBuilder;
        _builder = builder;
        _parser = parser;
    }

    protected override string ErrorPrefix => "query";

    public void Process(string? filter)
    {
        if (string.IsNullOrEmpty(filter))
            return;

        RqlExpression rql;
        var parseResult = _parser.Parse(filter);
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
            var lambda = (Expression<Func<TView, bool>>)Expression.Lambda(expression.Value, param);
            return q.Where(lambda);
        });
    }
}