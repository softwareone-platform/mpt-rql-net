using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering;

/// <summary>
/// Pushes a filter over the view down beneath the projection, onto its source.
/// <para>
/// The projection is copied into the filter, and every member the filter reads is taken straight from it:
/// <c>new T { M = e }.M</c> becomes <c>e</c>. The projection wraps each nullable reference in a null check,
/// <c>x != null ? new T { ... } : null</c>. The check is removed when the value it protects is read through <c>x</c>
/// itself, so <c>(x != null ? new T { M = x.M } : null).M</c> becomes <c>x.M</c>: where reading a member of a missing
/// reference gives null, the check makes no difference. Values computed in other ways keep their checks.
/// </para>
/// </summary>
internal sealed class FilterPushdownVisitor : ExpressionVisitor
{
    private static readonly MethodInfo _whereMethod =
        ((Func<IQueryable<object>, Expression<Func<object, bool>>, IQueryable<object>>)Queryable.Where).Method.GetGenericMethodDefinition();

    private readonly ParameterExpression _view;
    private readonly Expression _projection;

    private FilterPushdownVisitor(ParameterExpression view, Expression projection)
    {
        _view = view;
        _projection = projection;
    }

    /// <summary>
    /// Filters the source of a projected query: <c>source.Select(projection)</c> becomes
    /// <c>source.Where(filter).Select(projection)</c>, with the filter rewritten onto the source.
    /// A query that is not a projection is filtered as it is.
    /// </summary>
    public static IQueryable<TView> Apply<TView>(IQueryable<TView> query, Expression<Func<TView, bool>> filter)
    {
        if (query.Expression is not MethodCallExpression { Arguments: [var source, UnaryExpression { Operand: LambdaExpression { Parameters.Count: 1 } projection }] } select
            || select.Method.DeclaringType != typeof(Queryable)
            || select.Method.Name != nameof(Queryable.Select))
        {
            return query.Where(filter);
        }

        var where = Expression.Call(_whereMethod.MakeGenericMethod(projection.Parameters[0].Type), source, Expression.Quote(ToSourceFilter(filter, projection)));
        return query.Provider.CreateQuery<TView>(select.Update(null, [where, select.Arguments[1]]));
    }

    /// <summary>
    /// Rewrites a filter over the view into a filter over the parameter of the projection.
    /// </summary>
    public static LambdaExpression ToSourceFilter(LambdaExpression filter, LambdaExpression projection)
    {
        var body = new FilterPushdownVisitor(filter.Parameters[0], projection.Body).Visit(filter.Body);
        return Expression.Lambda(body, projection.Parameters[0]);
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _view ? _projection : node;

    protected override Expression VisitMember(MemberExpression node)
    {
        var source = Visit(node.Expression);

        return source != null && Bind(source, node.Member) is { } bound && bound.Type == node.Type
            ? bound
            : node.Update(source);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var visited = base.VisitBinary(node);

        // safe navigation compares projected objects with null; test what they are built from instead
        if (visited is BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } comparison
            && ComparedWithNull(comparison) is { } operand
            && IsNull(operand) is { } isNull)
        {
            return comparison.NodeType == ExpressionType.Equal ? isNull : Negate(isNull);
        }

        return visited;
    }

    private static Expression? ComparedWithNull(BinaryExpression comparison)
    {
        if (comparison.Right is ConstantExpression { Value: null })
            return comparison.Left;

        return comparison.Left is ConstantExpression { Value: null } ? comparison.Right : null;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var visited = base.VisitMethodCall(node);

        // any() and all() over a projected list read the same items without ToList(),
        // which some providers cannot translate inside a filter
        if (visited is MethodCallExpression { Object: null, Arguments: [MethodCallExpression toList, ..] } call
            && IsEnumerableToList(toList)
            && call.Method.DeclaringType == typeof(Enumerable))
        {
            return call.Update(null, [toList.Arguments[0], .. call.Arguments.Skip(1)]);
        }

        return visited;
    }

    /// <summary>
    /// Reads a member of an object the projection builds; null for any other source.
    /// </summary>
    private static Expression? Bind(Expression source, MemberInfo member)
    {
        if (source is MemberInitExpression init)
        {
            foreach (var binding in init.Bindings)
            {
                if (binding is MemberAssignment assignment && IsSameMember(assignment.Member, member))
                    return assignment.Expression;
            }

            return null;
        }

        // x != null ? new T { ... } : null, the null check the projection puts around a nullable reference
        if (source is ConditionalExpression
            {
                Test: BinaryExpression { NodeType: ExpressionType.NotEqual, Right: ConstantExpression { Value: null } } test,
                IfFalse: ConstantExpression { Value: null }
            } guard)
        {
            var bound = Bind(guard.IfTrue, member);
            if (bound == null)
                return null;

            if (IsMemberChainOf(bound, test.Left))
                return bound;

            // the check still matters: keep it, unless the member's type cannot hold null
            return bound.Type.IsValueType && Nullable.GetUnderlyingType(bound.Type) == null
                ? null
                : Expression.Condition(test, bound, Expression.Constant(null, bound.Type));
        }

        return null;
    }

    /// <summary>
    /// The test for <c>operand == null</c> when the projection builds the operand: a new object or list is never null,
    /// and an object behind a null check is null when the check fails. Null for any other operand.
    /// </summary>
    private static Expression? IsNull(Expression operand)
    {
        switch (operand)
        {
            case MemberInitExpression:
            case MethodCallExpression call when IsEnumerableToList(call):
                return Expression.Constant(false);

            case ConditionalExpression
            {
                Test: BinaryExpression { NodeType: ExpressionType.NotEqual, Right: ConstantExpression { Value: null } } test,
                IfFalse: ConstantExpression { Value: null }
            } guard:
                var guardFails = Expression.Equal(test.Left, test.Right);
                return IsNull(guard.IfTrue) switch
                {
                    null => null,
                    ConstantExpression { Value: false } => guardFails,
                    var valueIsNull => Expression.OrElse(guardFails, valueIsNull)
                };

            default:
                return null;
        }
    }

    private static Expression Negate(Expression condition) => condition switch
    {
        ConstantExpression { Value: bool value } => Expression.Constant(!value),
        BinaryExpression { NodeType: ExpressionType.Equal } equal => Expression.NotEqual(equal.Left, equal.Right),
        _ => Expression.Not(condition)
    };

    private static bool IsEnumerableToList(MethodCallExpression call)
        => call.Method.DeclaringType == typeof(Enumerable) && call.Method.Name == nameof(Enumerable.ToList);

    // expression is root itself or reads members of it, as x.A.B reads x
    private static bool IsMemberChainOf(Expression expression, Expression root)
    {
        while (!IsSameAccess(expression, root))
        {
            switch (expression)
            {
                case MemberExpression { Expression: { } inner }:
                    expression = inner;
                    break;
                case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert:
                    expression = convert.Operand;
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    // the same member path, such as x.A.B, whether or not the two share nodes
    private static bool IsSameAccess(Expression? left, Expression? right)
    {
        while (left != right)
        {
            if (left is not MemberExpression leftMember || right is not MemberExpression rightMember || !IsSameMember(leftMember.Member, rightMember.Member))
                return false;

            left = leftMember.Expression;
            right = rightMember.Expression;
        }

        return true;
    }

    private static bool IsSameMember(MemberInfo left, MemberInfo right)
        => left == right || (left.DeclaringType == right.DeclaringType && left.Name == right.Name);
}
