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

    public static Expression StartsWith(Expression member, string value, StringComparison? comparison = null)
    {
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            return Expression.Call(member, StartsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        return Expression.Call(member, StartsWithMethod, ConstantBuilder.Build(value, typeof(string)));
    }

    public static Expression EndsWith(Expression member, string value, StringComparison? comparison = null)
    {
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            return Expression.Call(member, EndsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        return Expression.Call(member, EndsWithMethod, ConstantBuilder.Build(value, typeof(string)));
    }

    public static Expression Contains(Expression member, string value, StringComparison? comparison = null)
    {
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            return Expression.Call(member, ContainsComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        return Expression.Call(member, ContainsMethod, ConstantBuilder.Build(value, typeof(string)));
    }

    public static Expression Equals(Expression member, string value, StringComparison? comparison = null)
    {
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            return Expression.Call(member, EqualsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType);
        }
        return Expression.Call(member, EqualsMethod, ConstantBuilder.Build(value, typeof(string)));
    }

    public static Expression NotEquals(Expression member, string value, StringComparison? comparison = null)
    {
        if (comparison.HasValue)
        {
            var comparisonType = Expression.Constant(comparison.Value, typeof(StringComparison));
            return Expression.Not(Expression.Call(member, EqualsWithComparisonMethod, ConstantBuilder.Build(value, typeof(string)), comparisonType));
        }
        return Expression.Not(Expression.Call(member, EqualsMethod, ConstantBuilder.Build(value, typeof(string))));
    }
}
