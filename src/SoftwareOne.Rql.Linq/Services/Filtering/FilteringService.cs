using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Abstractions.Unary;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Unary;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal delegate BinaryExpression LogicalExpression(Expression left, Expression right);

internal sealed class FilteringService<TView> : RqlService, IFilteringService<TView>
{
    private readonly IOperatorHandlerProvider _operatorHandlerProvider;
    private readonly IRqlParser _parser;

    public FilteringService(IOperatorHandlerProvider operatorHandlerProvider,
        IMetadataProvider typeMetadataProvider,
        IRqlParser parser) : base(typeMetadataProvider)
    {
        _operatorHandlerProvider = operatorHandlerProvider;
        _parser = parser;
    }

    protected override string ErrorPrefix => "query";

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
            _ => MakeInternalError()
        };
    }

    private ErrorOr<Expression> MakeGroupExpression(ParameterExpression pe, RqlGroup group)
    {
        var handler = group switch
        {
            RqlAnd => (ErrorOr<LogicalExpression>)Expression.AndAlso,
            RqlOr => (ErrorOr<LogicalExpression>)Expression.OrElse,
            RqlGenericGroup genGroup => Error.Validation(MakeErrorCode(genGroup.Name), "Unknown expression group."),
            _ => MakeInternalError()
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

        if (node.Left is not RqlConstant memberConstant)
            return Error.Validation(MakeErrorCode("unknown"), "Unknown property were used for binary expression.");

        var member = MakeMemberAccess(pe, memberConstant.Value, static path =>
        {
            if (!path.PropertyInfo.Actions.HasFlag(RqlAction.Filter))
                return Error.Validation(description: "Filtering is not permitted");
            return Result.Success;
        });

        if (member.IsError)
            return AssignErrorCode(member.Errors, MakeErrorCode(memberConstant.Value));

        var expression = handler switch
        {
            IComparisonOperator comp => BinaryExpressionFactory.MakeSimple(node, true, value => comp.MakeExpression(member.Value, value)),
            ISearchOperator search => BinaryExpressionFactory.MakeSimple(node, false, value => search.MakeExpression(member.Value, value!)),
            IListOperator list => BinaryExpressionFactory.MakeList(node, member.Value, list),
            _ => MakeInternalError()
        };

        return expression.IsError ? AssignErrorCode(expression.Errors, MakeErrorCode(memberConstant.Value)) : expression;

        static List<Error> AssignErrorCode(IEnumerable<Error> errors, string code)
            => errors.Select(s => Error.Validation(code, s.Description)).ToList();
    }

    private ErrorOr<Expression> MakeUnaryExpression(ParameterExpression pe, RqlUnary node)
    {
        var handler = (IUnaryOperator)_operatorHandlerProvider.GetOperatorHandler(node.GetType())!;
        var expression = MakeFilterExpression(pe, node.Nested);
        return expression.Match(handler.MakeExpression, errors => errors);
    }

    private Error MakeInternalError() => Error.Failure(MakeErrorCode("internal"), "Internal filtering error occurred. Please contact RQL package maintainer.");
}