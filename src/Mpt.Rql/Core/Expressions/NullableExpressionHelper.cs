using System.Linq.Expressions;

namespace Mpt.Rql.Core.Expressions;

/// <summary>
/// Shared null-handling idioms for expression trees. Used by path building (safe navigation),
/// filtering (operator null-safety), mapping (conditional inits) and ordering functions.
/// </summary>
internal static class NullableExpressionHelper
{
    /// <summary>
    /// Converts a non-nullable value-type expression to <see cref="Nullable{T}"/> so that <c>null</c>
    /// becomes representable; reference types and already-nullable types are returned unchanged.
    /// </summary>
    public static Expression LiftToNullable(Expression expression)
    {
        var type = expression.Type;
        if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            return Expression.Convert(expression, typeof(Nullable<>).MakeGenericType(type));

        return expression;
    }

    /// <summary>
    /// Builds <c>guard == null ? null : value</c>, lifting <paramref name="value"/> to a nullable type when needed.
    /// </summary>
    public static Expression NullGuard(Expression guard, Expression value)
    {
        var lifted = LiftToNullable(value);

        return Expression.Condition(
            Expression.Equal(guard, Expression.Constant(null, guard.Type)),
            Expression.Constant(null, lifted.Type),
            lifted);
    }
}
