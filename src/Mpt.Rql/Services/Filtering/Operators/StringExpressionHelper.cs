using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core.Expressions;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Operators;

internal static class StringExpressionHelper
{
    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!;
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
    private static readonly MethodInfo EqualsMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string)])!;
    private static readonly MethodInfo EqualsWithComparisonMethod = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo StartsWithComparisonMethod = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo EndsWithComparisonMethod = typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string), typeof(StringComparison)])!;
    private static readonly MethodInfo ContainsComparisonMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;

    public static Expression StartsWith(this Expression accessor, string value, IRqlSettings settings)
    {
        var comparison = settings.Filter.Strings.Comparison;

        Expression expression;
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            expression = Expression.Call(accessor, StartsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        else
            expression = Expression.Call(accessor, StartsWithMethod, ConstantBuilder.Build(value, typeof(string)));

        return WithNullSafetyIfEnabled(expression, accessor, settings);
    }

    public static Expression EndsWith(this Expression accessor, string value, IRqlSettings settings)
    {
        var comparison = settings.Filter.Strings.Comparison;

        Expression expression;
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            expression = Expression.Call(accessor, EndsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        else
            expression = Expression.Call(accessor, EndsWithMethod, ConstantBuilder.Build(value, typeof(string)));

        return WithNullSafetyIfEnabled(expression, accessor, settings);
    }

    public static Expression Contains(this Expression accessor, string value, IRqlSettings settings)
    {
        var comparison = settings.Filter.Strings.Comparison;

        Expression expression;
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            expression = Expression.Call(accessor, ContainsComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        else
            expression = Expression.Call(accessor, ContainsMethod, ConstantBuilder.Build(value, typeof(string)));

        return WithNullSafetyIfEnabled(expression, accessor, settings);
    }

    public static Expression Equals(this Expression accessor, string value, IRqlSettings settings)
    {
        var comparison = settings.Filter.Strings.Comparison;

        Expression expression;
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            expression = Expression.Call(accessor, EqualsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        else
            expression = Expression.Call(accessor, EqualsMethod, ConstantBuilder.Build(value, typeof(string)));

        return WithNullSafetyIfEnabled(expression, accessor, settings);
    }

    public static Expression NotEquals(this Expression accessor, string value, IRqlSettings settings)
    {
        var comparison = settings.Filter.Strings.Comparison;

        Expression expression;
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            expression = Expression.Not(Expression.Call(accessor, EqualsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType));
        }
        else
            expression = Expression.Not(Expression.Call(accessor, EqualsMethod, ConstantBuilder.Build(value, typeof(string))));

        return WithNullSafetyIfEnabled(expression, accessor, settings);
    }

    public static Expression WithNullSafetyIfEnabled(this Expression expression, Expression accessor, IRqlSettings settings)
    {
        if (settings.Filter.SafeNavigation != SafeNavigationMode.On)
            return expression;

        var nullConstant = Expression.Constant(null, typeof(string));

        return Expression.AndAlso(
            Expression.NotEqual(accessor, nullConstant),
            expression
        );
    }
}
