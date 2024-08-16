using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Argument.Pointer;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Linq.Core.Result;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Builders;

internal class BinaryExpressionBuilder : IConcreteExpressionBuilder<RqlBinary>
{
    private readonly IFilteringPathInfoBuilder _pathBuilder;
    private readonly IOperatorHandlerProvider _operatorHandlerProvider;

    public BinaryExpressionBuilder(IOperatorHandlerProvider operatorHandlerProvider, IFilteringPathInfoBuilder pathBuilder)
    {
        _pathBuilder = pathBuilder;
        _operatorHandlerProvider = operatorHandlerProvider;
    }

    public Result<Expression> Build(ParameterExpression pe, RqlBinary node)
    {
        var handler = _operatorHandlerProvider.GetOperatorHandler(node.GetType())!;

        var memberInfo = _pathBuilder.Build(pe, node.Left);

        if (memberInfo.IsError)
            return memberInfo.Errors;

        var property = memberInfo.Value!.PropertyInfo;
        var accessor = memberInfo.Value.Expression;

        var expression = handler switch
        {
            IComparisonOperator comp => MakeComparison(pe, node, property, accessor, comp),
            ISearchOperator search => MakeSearch(node, property, accessor, search),
            IListOperator list => MakeList(node, property, accessor, list),
            _ => FilteringError.Internal
        };

        return expression.IsError ? expression.Errors : expression;
    }

    private Result<Expression> MakeComparison(ParameterExpression parameter, RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, IComparisonOperator comparison)
    {
        if (node.Right is RqlPointer pointer)
        {
            var rightExpression = _pathBuilder.Build(parameter, pointer);
            if (rightExpression.IsError)
                return rightExpression.Errors;

            return ((ComparisonOperator)comparison).Handler.Invoke(accessor, Expression.ConvertChecked(rightExpression.Value!.Expression, accessor.Type));
        }
        else
        {
            var arg = GetRightConstantArgument(node.Right, true);
            if (arg.IsError)
                return arg.Errors;

            return comparison.MakeExpression(propertyInfo, accessor, arg.Value);
        }
    }

    private static Result<Expression> MakeSearch(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, ISearchOperator search)
    {
        if (accessor is not MemberExpression member)
            return Error.General("Search operations work with properties only");

        var arg = GetRightConstantArgument(node.Right, false);
        if (arg.IsError)
            return arg.Errors;

        return search.MakeExpression(propertyInfo, member, arg.Value!);
    }

    private static Result<Expression> MakeList(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, IListOperator list)
    {
        if (accessor is not MemberExpression member)
            return Error.General("List operations work with properties only");

        if (node.Right is not RqlGroup grp || grp.Items == null || grp.Items.Count == 0)
            return Error.Validation("Value has to be a non empty array.");

        var values = grp.Items.Select(x => GetRightConstantArgument(x, false)).ToList();

        if (values.Exists(t => t.IsError))
            return values.SelectMany(s => s.Errors).ToList();

        return list.MakeExpression(propertyInfo, member, values.Select(s => s.Value!));
    }

    private static Result<string?> GetRightConstantArgument(RqlExpression right, bool allowNull)
    {
        Result<string?> res = right switch
        {
            RqlNull => (string?)null,
            RqlEmpty => string.Empty,
            RqlConstant str => str.Value,
            _ => Error.Validation("Unsupported argument type.")
        };

        if (!allowNull && !res.IsError && res.Value == null)
            return Error.Validation("Null values are not supported.");

        return res;
    }
}