using ErrorOr;
using SoftwareOne.Rql.Abstractions.Group;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

internal class GroupExpressionBuilder : IConcreteExpressionBuilder<RqlGroup>
{
    private readonly IExpressionBuilder _builder;

    public GroupExpressionBuilder(IExpressionBuilder builder)
    {
        _builder = builder;
    }

    public ErrorOr<Expression> Build(ParameterExpression pe, RqlGroup node)
    {
        var handler = node switch
        {
            RqlAnd => (ErrorOr<LogicalExpression>)Expression.AndAlso,
            RqlOr => (ErrorOr<LogicalExpression>)Expression.OrElse,
            RqlGenericGroup genGroup => Error.Validation(genGroup.Name, "Unknown expression group."),
            _ => FilteringError.Internal
        };

        if (handler.IsError)
            return handler.Errors;

        var errors = new List<Error>();
        var filter = _builder.Build(pe, node.Items![0]);

        if (filter.IsError)
            errors.AddRange(filter.Errors);

        for (var i = 1; i < node.Items.Count; i++)
        {
            var innerFilter = _builder.Build(pe, node.Items[i]);

            if (innerFilter.IsError)
                errors.AddRange(innerFilter.Errors);

            if (!filter.IsError && !innerFilter.IsError)
                filter = handler.Value(filter.Value, innerFilter.Value);
        }
        return errors.Any() ? errors : filter;
    }
}