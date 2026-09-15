using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Filtering.Operators;
using Mpt.Rql.Services.Filtering.Operators.Comparison;
using Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;
using Mpt.Rql.Services.Filtering.Operators.List;
using Mpt.Rql.Services.Filtering.Operators.Search;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Filtering.Builders;

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

    /// <summary>
    /// A right-hand path whose type cannot be coerced to the left side (e.g. Guid vs string) is not a usable
    /// property comparison; the caller then treats the text as a literal instead of throwing.
    /// </summary>
    private static bool TryConvertChecked(Expression expression, Type targetType, out Expression converted)
    {
        try
        {
            converted = Expression.ConvertChecked(expression, targetType);
            return true;
        }
        catch (InvalidOperationException)
        {
            converted = expression;
            return false;
        }
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

        // Try to interpret right side as property path if it's an unquoted constant
        // Prefer property path over constant value when name matches a property
        if (node.Right is RqlConstant constant && !string.IsNullOrEmpty(constant.Value) && !constant.IsQuoted)
        {
            var rightAsProperty = _pathBuilder.Build(parameter, constant.Value);
            if (!rightAsProperty.IsError && TryConvertChecked(rightAsProperty.Value!.Expression, accessor.Type, out var rightExpression))
            {
                // Successfully resolved as a property path of a compatible type
                return ((ComparisonOperator)comparison).Handler.Invoke(accessor, rightExpression);
            }
        }

        // Fallback to treating right side as constant
        var arg = GetRightConstantArgument(node.Right, true);
        if (arg.IsError)
            return arg.Errors;

        return comparison.MakeExpression(propertyInfo, accessor, arg.Value);
    }

    private static Result<Expression> MakeSearch(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, ISearchOperator search)
    {
        var arg = GetRightConstantArgument(node.Right, false);
        if (arg.IsError)
            return arg.Errors;

        return search.MakeExpression(propertyInfo, accessor, arg.Value!);
    }

    private static Result<Expression> MakeList(RqlBinary node, IRqlPropertyInfo propertyInfo, Expression accessor, IListOperator list)
    {
        if (node.Right is not RqlGroup grp || grp.Items == null || grp.Items.Count == 0)
            return Error.Validation("Value has to be a non empty array.");

        var values = grp.Items.Select(x => GetRightConstantArgument(x, false)).ToList();

        if (values.Exists(t => t.IsError))
            return values.SelectMany(s => s.Errors).ToList();

        return list.MakeExpression(propertyInfo, accessor, values.Select(s => s.Value!));
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