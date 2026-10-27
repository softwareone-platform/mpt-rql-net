using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Core;

/// <summary>
/// Provides shared collection type and query-expression operations.
/// </summary>
internal static class CollectionQueryHelper
{
    public static readonly MethodInfo QueryableSelect =
        ((Func<
            IQueryable<object>,
            Expression<Func<object, object>>,
            IQueryable<object>>)Queryable.Select)
        .Method.GetGenericMethodDefinition();

    public static readonly MethodInfo QueryableWhere =
        ((Func<
            IQueryable<object>,
            Expression<Func<object, bool>>,
            IQueryable<object>>)Queryable.Where)
        .Method.GetGenericMethodDefinition();

    public static readonly MethodInfo EnumerableWhere =
        ((Func<
            IEnumerable<object>,
            Func<object, bool>,
            IEnumerable<object>>)Enumerable.Where)
        .Method.GetGenericMethodDefinition();

    /// <summary>
    /// Gets distinct generic enumerable element types exposed by a type.
    /// </summary>
    public static Type[] GetEnumerableElementTypes(Type collectionType)
    {
        return collectionType
            .GetInterfaces()
            .Append(collectionType)
            .Where(candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(candidate => candidate.GetGenericArguments()[0])
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Determines whether a call is a supported Enumerable quantifier.
    /// </summary>
    public static bool IsQuantifier(MethodCallExpression expression)
    {
        return expression.Method.DeclaringType == typeof(Enumerable) &&
            expression.Method.Name is
                nameof(Enumerable.Any) or nameof(Enumerable.All) &&
            expression.Arguments.Count is 1 or 2;
    }

    /// <summary>
    /// Determines whether an expression guards a quantifier's collection.
    /// </summary>
    public static bool IsGuardedQuantifier(BinaryExpression expression)
    {
        return expression.NodeType == ExpressionType.AndAlso &&
            expression.Left is BinaryExpression
            {
                NodeType: ExpressionType.NotEqual
            } comparison &&
            ComparedWithNull(comparison) is { } collection &&
            expression.Right is MethodCallExpression quantifier &&
            IsQuantifier(quantifier) &&
            IsSameAccess(collection, quantifier.Arguments[0]);
    }

    /// <summary>
    /// Gets the operand compared with a null constant.
    /// </summary>
    public static Expression? ComparedWithNull(BinaryExpression comparison)
    {
        if (comparison.Right is ConstantExpression { Value: null })
            return comparison.Left;

        return comparison.Left is ConstantExpression { Value: null }
            ? comparison.Right
            : null;
    }

    /// <summary>
    /// Determines whether expressions represent the same member access path.
    /// </summary>
    public static bool IsSameAccess(Expression? left, Expression? right)
    {
        while (left != right)
        {
            if (left is not MemberExpression leftMember ||
                right is not MemberExpression rightMember ||
                !IsSameMember(leftMember.Member, rightMember.Member))
            {
                return false;
            }

            left = leftMember.Expression;
            right = rightMember.Expression;
        }

        return true;
    }

    /// <summary>
    /// Determines whether reflection members identify the same declared member.
    /// </summary>
    public static bool IsSameMember(MemberInfo left, MemberInfo right)
    {
        return left == right ||
            (left.DeclaringType == right.DeclaringType &&
             left.Name == right.Name);
    }
}