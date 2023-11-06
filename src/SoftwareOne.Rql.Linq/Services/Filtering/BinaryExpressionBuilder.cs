using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Argument.Pointer;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal interface IBinaryExpressionBuilder
{
    ErrorOr<Expression> MakeComparison(ParameterExpression parameter, RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, IComparisonOperator comparison);

    ErrorOr<Expression> MakeSearch(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, ISearchOperator search);

    ErrorOr<Expression> MakeList(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, IListOperator list);
}

internal class BinaryExpressionBuilder : IBinaryExpressionBuilder
{
    private readonly IFilteringPathInfoBuilder _pathBuilder;

    public BinaryExpressionBuilder(IFilteringPathInfoBuilder pathBuilder)
    {
        _pathBuilder = pathBuilder;
    }

    public ErrorOr<Expression> MakeComparison(ParameterExpression parameter, RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, IComparisonOperator comparison)
    {
        if (node.Right is RqlPointer pointer)
        {
            var rightExpression = _pathBuilder.Build(parameter, pointer);
            if (rightExpression.IsError)
                return rightExpression.Errors;

            return ((ComparisonOperator)comparison).Handler.Invoke(accessor, Expression.ConvertChecked(rightExpression.Value.Expression, accessor.Type));
        }
        else
        {
            var arg = GetRightConstantArgument(node.Right, true);
            return comparison.MakeExpression(propertyInfo, accessor, arg.Value);
        }
    }

    public ErrorOr<Expression> MakeSearch(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, ISearchOperator search)
    {
        if (accessor is not MemberExpression member)
            return Error.Failure(description: "Search operations work with properties only");

        var arg = GetRightConstantArgument(node.Right, false);
        return search.MakeExpression(propertyInfo, member, arg.Value!);
    }

    public ErrorOr<Expression> MakeList(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, IListOperator list)
    {
        if (accessor is not MemberExpression member)
            return Error.Failure(description: "List operations work with properties only");

        if (node.Right is not RqlGroup grp || grp.Items == null || grp.Items.Count == 0)
            return Error.Validation(description: "Value has to be a non empty array.");

        var values = grp.Items.Select(x => GetRightConstantArgument(x, false)).ToList();

        if (values.Exists(t => t.IsError))
            return values.SelectMany(s => s.Errors).ToList();

        return list.MakeExpression(propertyInfo, member, values.Select(s => s.Value!));
    }

    private static ErrorOr<string?> GetRightConstantArgument(RqlExpression right, bool allowNull)
    {
        ErrorOr<string?> res = right switch
        {
            RqlNull => (string?)null,
            RqlEmpty => string.Empty,
            RqlConstant str => str.Value,
            _ => Error.Validation(description: "Unsupported argument type.")
        };

        if (!allowNull && !res.IsError && res.Value == null)
            return Error.Validation(description: "Null values are not supported.");

        return res;
    }
}