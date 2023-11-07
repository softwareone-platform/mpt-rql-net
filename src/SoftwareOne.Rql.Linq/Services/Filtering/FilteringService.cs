using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Linq.Services.Filtering.Builders;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal delegate BinaryExpression LogicalExpression(Expression left, Expression right);

internal sealed class FilteringService<TView> : IFilteringService<TView>
{
    private readonly IExpressionBuilder _builder;
    private readonly IRqlParser _parser;

    public FilteringService(IExpressionBuilder builder, IRqlParser parser)
    {
        _builder = builder;
        _parser = parser;
    }

    public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? filter)
    {
        if (string.IsNullOrEmpty(filter))
            return ErrorOrFactory.From(query);

        RqlExpression rql;
        var parseResult = _parser.Parse(filter);
        if (parseResult.Items?.Count == 1 && parseResult is RqlGenericGroup genGrp && genGrp.Name == string.Empty)
            rql = parseResult.Items[0];
        else
            rql = parseResult;

        var param = Expression.Parameter(typeof(TView));
        var expression = _builder.Build(param, rql);

        if (expression.IsError)
            return expression.Errors;

        var lambda = (Expression<Func<TView, bool>>)Expression.Lambda(expression.Value, param);

        return ErrorOrFactory.From(query.Where(lambda));
    }
}