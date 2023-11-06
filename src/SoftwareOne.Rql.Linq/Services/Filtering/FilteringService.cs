using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Abstractions.Unary;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal delegate BinaryExpression LogicalExpression(Expression left, Expression right);

internal sealed class FilteringService<TView> : IFilteringService<TView>
{
    private readonly IOperatorHandlerProvider _operatorHandlerProvider;
    private readonly IBinaryExpressionBuilder _binaryExpressionBuilder;
    private readonly IFilteringPathInfoBuilder _pathBuilder;
    private readonly IRqlParser _parser;

    public FilteringService(IOperatorHandlerProvider operatorHandlerProvider,
        IFilteringPathInfoBuilder pathBuilder,
        IBinaryExpressionBuilder binaryExpressionBuilder, IRqlParser parser)
    {
        _operatorHandlerProvider = operatorHandlerProvider;
        _binaryExpressionBuilder = binaryExpressionBuilder;
        _pathBuilder = pathBuilder;
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
        var expression = MakeFilterExpression(param, rql);

        if (expression.IsError)
            return expression.Errors;

        var lambda = (Expression<Func<TView, bool>>)Expression.Lambda(expression.Value, param);

        return ErrorOrFactory.From(query.Where(lambda));
    }

    private ErrorOr<Expression> MakeFilterExpression(ParameterExpression pe, RqlExpression rql)
    {
        return rql switch
        {
            RqlGroup group => MakeGroupExpression(pe, group),
            RqlBinary binary => MakeBinaryExpression(pe, binary),
            RqlUnary unary => MakeUnaryExpression(pe, unary),
            _ => FilteringError.Internal
        };
    }

    private ErrorOr<Expression> MakeGroupExpression(ParameterExpression pe, RqlGroup group)
    {
        var handler = group switch
        {
            RqlAnd => (ErrorOr<LogicalExpression>)Expression.AndAlso,
            RqlOr => (ErrorOr<LogicalExpression>)Expression.OrElse,
            RqlGenericGroup genGroup => Error.Validation(genGroup.Name, "Unknown expression group."),
            _ => FilteringError.Internal
        };

        if (handler.IsError)
            return handler.Errors;

        var errors = new List<Error>();
        var filter = MakeFilterExpression(pe, group.Items![0]);

        if (filter.IsError)
            errors.AddRange(filter.Errors);

        for (var i = 1; i < group.Items.Count; i++)
        {
            var innerFilter = MakeFilterExpression(pe, group.Items[i]);

            if (innerFilter.IsError)
                errors.AddRange(innerFilter.Errors);

            if (!filter.IsError && !innerFilter.IsError)
                filter = handler.Value(filter.Value, innerFilter.Value);
        }
        return errors.Any() ? errors : filter;
    }

    private ErrorOr<Expression> MakeBinaryExpression(ParameterExpression pe, RqlBinary node)
    {
        var handler = _operatorHandlerProvider.GetOperatorHandler(node.GetType())!;

        var memberInfo = _pathBuilder.Build(pe, node.Left);

        if (memberInfo.IsError)
            return memberInfo.Errors;

        var property = memberInfo.Value.PropertyInfo;
        var accessor = memberInfo.Value.Expression;

        var expression = handler switch
        {
            IComparisonOperator comp => _binaryExpressionBuilder.MakeComparison(pe, node, property, accessor, comp),
            ISearchOperator search => _binaryExpressionBuilder.MakeSearch(node, property, accessor, search),
            IListOperator list => _binaryExpressionBuilder.MakeList(node, property, accessor, list),
            ICollectionOperator sub => MakeCollectionExpression(node, property, accessor, sub),
            _ => FilteringError.Internal
        };

        return expression.IsError ? expression.Errors : expression;
    }

    private ErrorOr<Expression> MakeCollectionExpression(RqlBinary node, RqlPropertyInfo propertyInfo, Expression accessor, ICollectionOperator handler)
    {
        if (accessor is not MemberExpression member)
            return Error.Failure(description: "Collection operations work with properties only");

        if (propertyInfo.ElementType == null)
            return Error.Failure(description: "Collection property has incompatible type");

        var param = Expression.Parameter(propertyInfo.ElementType);
        var innerExpression = MakeFilterExpression(param, node.Right);

        if (innerExpression.IsError)
            return innerExpression.Errors;

        var lambda = Expression.Lambda(innerExpression.Value, param);

        return handler.MakeExpression(propertyInfo, member, lambda);
    }

    private ErrorOr<Expression> MakeUnaryExpression(ParameterExpression pe, RqlUnary node)
    {
        var handler = (IUnaryOperator)_operatorHandlerProvider.GetOperatorHandler(node.GetType())!;
        var expression = MakeFilterExpression(pe, node.Nested);
        return expression.Match(handler.MakeExpression, errors => errors);
    }
}