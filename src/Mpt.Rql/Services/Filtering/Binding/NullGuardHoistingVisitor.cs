using Mpt.Rql.Core.Expressions;
using Mpt.Rql.Services.Mapping;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Binding;

/// <summary>
/// Simplifies a predicate in which the view parameter has been replaced by the mapping projection.
/// <para>
/// The projection guards every nullable nested reference (<c>x != null ? new T { ... } : null</c>) and safe
/// navigation guards every path segment (<c>x == null ? null : x.M</c>). Left as they are, a member access or a
/// comparison over such a guard reaches a relational provider as a conditional around the column
/// (<c>CASE WHEN ... END = @value</c>), which defeats indexes. This visitor binds member accesses to the
/// projection (<c>new T { M = e }.M</c> becomes <c>e</c>), pushes guards outwards through member accesses and
/// turns a guarded operand into boolean terms around a plain operand, e.g. <c>(g ? e : null) == v</c> becomes
/// <c>g &amp;&amp; e == v</c> and <c>(g ? e : null) != v</c> becomes <c>!g || e != v</c>.
/// </para>
/// <para>
/// Every rewrite preserves the C# meaning of the original expression: a failed guard yields <c>null</c>, and the
/// rewrite spells out what the surrounding operator does with that <c>null</c> (equality, lifted comparisons);
/// where the original would dereference that <c>null</c> (a method call, a member access) the rewrite yields
/// <c>false</c> or <c>null</c> instead of throwing. The result is therefore valid for LINQ-to-Objects as well.
/// Shapes that are not recognised — a conditional with two non-null branches, a guard on both sides of an
/// equality, static method calls other than the <see cref="Enumerable"/> ones used by RQL — are kept as they
/// are (apart from re-typing lifted operands); they stay correct, only not simplified.
/// </para>
/// </summary>
internal sealed class NullGuardHoistingVisitor : ExpressionVisitor
{
    private static readonly MethodInfo _toList = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!;
    private static readonly MethodInfo _select = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(Enumerable.Select) && m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var test = Visit(node.Test);
        var ifTrue = Visit(node.IfTrue);
        var ifFalse = Visit(node.IfFalse);

        // A branch that reached a member through a guard is lifted to its nullable type (see VisitMember), so the
        // other branch — a two-valued MapIf conditional, say — is lifted alongside it to keep the node well-typed.
        if (ifTrue.Type != ifFalse.Type)
        {
            ifTrue = IsNull(ifTrue) ? Expression.Constant(null, NullableExpressionHelper.LiftToNullable(ifFalse).Type) : NullableExpressionHelper.LiftToNullable(ifTrue);
            ifFalse = IsNull(ifFalse) ? Expression.Constant(null, ifTrue.Type) : NullableExpressionHelper.LiftToNullable(ifFalse);
        }

        var visited = ifTrue.Type == node.Type && ifFalse.Type == node.Type
            ? node.Update(test, ifTrue, ifFalse)
            : Expression.Condition(test, ifTrue, ifFalse);

        // normalises `!g ? null : e` to `g ? e : null` and flattens nested guards (see MakeGuard)
        return TryGetGuard(visited, out var condition, out var value) ? MakeGuard(condition, value) : visited;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is null)
            return base.VisitMember(node);

        // Visiting the operand resolves a path step by step (view.Case.Reporter: the projection binding of Case
        // is a guard around an initialiser, in which Reporter is bound); only the binding of each member is
        // visited, the other bindings of the initialiser are irrelevant to the predicate.
        var operand = Visit(node.Expression);
        if (ResolveMember(operand, node.Member) is { } resolved)
            return resolved;

        // (g ? e : null).M  =>  (g ? e.M : null)
        return TryGetGuard(operand, out var condition, out var value) && IsDeclaredOn(node.Member, value)
            ? MakeGuard(condition, Expression.MakeMemberAccess(value, node.Member))
            : node.Update(operand);
    }

    private static bool IsDeclaredOn(MemberInfo member, Expression expression)
        => member.DeclaringType is { } declaringType && declaringType.IsAssignableFrom(expression.Type);

    /// <summary>
    /// Object initialisers are the projection substituted for the view parameter. Their bindings are only
    /// reached through <see cref="ResolveMember"/>; an initialiser that is left in the predicate (an unbound
    /// member, a guard on both sides of an equality) is kept as the provider gets it today.
    /// </summary>
    protected override Expression VisitMemberInit(MemberInitExpression node) => node;

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);

        if (node.NodeType is not (ExpressionType.Convert or ExpressionType.ConvertChecked))
            return node.Update(operand);

        // pushing a guard through a member access lifts the member, which can make a lift redundant...
        if (operand.Type == node.Type)
            return operand;

        // ...or put a guard under a conversion: (T)(g ? e : null)  =>  (g ? (T)e : null)
        if (TryGetGuard(operand, out var condition, out var value))
            return MakeGuard(condition, value.Type == node.Type ? value : Expression.MakeUnary(node.NodeType, value, node.Type, node.Method));

        return operand == node.Operand ? node : Expression.MakeUnary(node.NodeType, operand, node.Type, node.Method);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        return node.NodeType switch
        {
            ExpressionType.Equal or ExpressionType.NotEqual => RewriteEquality(node, left, right),
            ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual
                => RewriteRelational(node, left, right),
            ExpressionType.AndAlso => And(left, right),
            _ => node.Update(left, node.Conversion, right)
        };
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var instance = node.Object is null ? null : Visit(node.Object);
        var arguments = Visit(node.Arguments);

        // (g ? e : null).Method(args)  =>  g && e.Method(args)   (bool) /  (g ? e.Method(args) : null)   (other)
        if (instance != null && TryGetGuard(instance, out var condition, out var receiver))
        {
            var call = Expression.Call(receiver, node.Method, arguments);
            return call.Type == typeof(bool) ? And(condition, call) : MakeGuard(condition, call);
        }

        if (node.Method.DeclaringType == typeof(Enumerable) && node.Method.IsGenericMethod)
        {
            var rewritten = node.Method.Name switch
            {
                nameof(Enumerable.Contains) when arguments.Count == 2 => RewriteContains(node, arguments),
                nameof(Enumerable.Any) or nameof(Enumerable.All) when arguments.Count is 1 or 2 => RewriteCollectionCall(node, arguments),
                _ => null
            };

            if (rewritten != null)
                return rewritten;
        }

        return node.Update(instance, arguments);
    }

    /// <summary>
    /// <c>list.Contains(g ? e : null)</c> becomes <c>g &amp;&amp; list.Contains(e)</c>. A value-type member reached
    /// through a guard was lifted on the way (see <see cref="VisitMember"/>) while RQL builds the list of the plain
    /// type, so the lift is undone; a call whose item still does not fit the list is left as it was. Returns
    /// <c>null</c> when the item is not guarded.
    /// </summary>
    private static Expression? RewriteContains(MethodCallExpression node, ReadOnlyCollection<Expression> arguments)
    {
        if (!TryGetGuard(arguments[1], out var condition, out var item))
            return null;

        var elementType = node.Method.GetGenericArguments()[0];
        if (item.Type != elementType && item is UnaryExpression { NodeType: ExpressionType.Convert, Method: null } lift && lift.Operand.Type == elementType)
            item = lift.Operand;

        return item.Type == elementType ? And(condition, Expression.Call(node.Method, arguments[0], item)) : node;
    }

    /// <summary>
    /// Resolves <c>operand.member</c> against an object initialiser (whose bindings are still unvisited, see
    /// <see cref="VisitMemberInit"/>), a reference conversion of one, or a guard around one: returns the visited
    /// binding of the member, with the guards pushed outwards (<c>(g ? new T { M = e } : null).M</c> becomes
    /// <c>(g ? e : null)</c>), or <c>null</c> when the operand is not such a shape or the member is not bound.
    /// </summary>
    private Expression? ResolveMember(Expression operand, MemberInfo member)
    {
        switch (operand)
        {
            case MemberInitExpression init:
                foreach (var binding in init.Bindings)
                {
                    if (binding is MemberAssignment assignment && SameMember(assignment.Member, member))
                        return Visit(assignment.Expression);
                }

                return null;

            case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert
                when !convert.Type.IsValueType && !convert.Operand.Type.IsValueType && IsDeclaredOn(member, convert.Operand):
                return ResolveMember(convert.Operand, member);
        }

        if (TryGetGuard(operand, out var condition, out var value) && ResolveMember(value, member) is { } resolved)
            return MakeGuard(condition, resolved);

        return null;
    }

    private static Expression RewriteEquality(BinaryExpression node, Expression left, Expression right)
    {
        var leftIsGuard = TryGetGuard(left, out var leftCondition, out var leftValue);
        var rightIsGuard = TryGetGuard(right, out var rightCondition, out var rightValue);

        // Two guards would need a four-way case analysis; the shape is rare (self() across two nullable
        // references), so it is left as it is.
        if (leftIsGuard == rightIsGuard)
            return Rebuild(node, left, right);

        var (condition, value, other) = leftIsGuard ? (leftCondition, leftValue, right) : (rightCondition, rightValue, left);
        var comparison = leftIsGuard ? MakeComparison(node, value, other) : MakeComparison(node, other, value);
        var isEqual = node.NodeType == ExpressionType.Equal;

        if (IsNull(other))
            return RewriteComparisonWithNull(isEqual, condition, value, comparison);

        // Against a value that is never null, equality needs the guard to hold as well, and inequality already
        // holds when the guard fails.
        if (IsKnownNonNull(other))
            return isEqual ? And(condition, comparison) : Expression.OrElse(Negate(condition), comparison);

        return RewriteComparisonWithNullableOperand(isEqual, condition, other, comparison);
    }

    /// <summary>
    /// Compared with <c>null</c>, a failed guard counts as <c>null</c>: equality holds when the guard fails or the
    /// value is <c>null</c>, inequality needs the guard and a non-null value. A value that can never be
    /// <c>null</c> (an object initialiser) reduces the comparison to the guard itself.
    /// </summary>
    private static Expression RewriteComparisonWithNull(bool isEqual, Expression condition, Expression value, BinaryExpression comparison)
    {
        if (IsNeverNull(value))
            return isEqual ? Negate(condition) : condition;

        return isEqual ? Expression.OrElse(Negate(condition), comparison) : Expression.AndAlso(condition, comparison);
    }

    /// <summary>
    /// The other operand may itself be <c>null</c> at run time, so when the guard fails the comparison is the one
    /// between <c>null</c> and that operand.
    /// </summary>
    private static BinaryExpression RewriteComparisonWithNullableOperand(bool isEqual, Expression condition, Expression other, BinaryExpression comparison)
    {
        var otherLifted = NullableExpressionHelper.LiftToNullable(other);
        var otherNull = Expression.Constant(null, otherLifted.Type);
        var whenGuardFails = isEqual ? Expression.Equal(otherLifted, otherNull) : Expression.NotEqual(otherLifted, otherNull);

        return Expression.OrElse(
            And(condition, comparison),
            Expression.AndAlso(Negate(condition), whenGuardFails));
    }

    // (g ? e : null) < v  =>  g && e < v : a lifted comparison with a null operand is false.
    private static Expression RewriteRelational(BinaryExpression node, Expression left, Expression right)
    {
        var leftIsGuard = TryGetGuard(left, out var leftCondition, out var leftValue);
        var rightIsGuard = TryGetGuard(right, out var rightCondition, out var rightValue);

        if (!leftIsGuard && !rightIsGuard)
            return Rebuild(node, left, right);

        Expression result = MakeComparison(node, leftIsGuard ? leftValue : left, rightIsGuard ? rightValue : right);

        if (rightIsGuard)
            result = And(rightCondition, result);
        if (leftIsGuard)
            result = And(leftCondition, result);

        return result;
    }

    /// <summary>
    /// <c>Any</c>/<c>All</c> over a projected collection: <c>(g ? list : null).Any(p)</c> becomes
    /// <c>g &amp;&amp; list.Any(p)</c>, and <c>src.Select(sel).ToList().Any(p)</c> becomes <c>src.Any(p ∘ sel)</c>
    /// with the element predicate bound to the element projection.
    /// </summary>
    private Expression RewriteCollectionCall(MethodCallExpression node, ReadOnlyCollection<Expression> arguments)
    {
        var source = arguments[0];
        var hasGuard = TryGetGuard(source, out var condition, out var guardedSource);
        if (hasGuard)
            source = guardedSource;

        if (source is MethodCallExpression toList && IsGenericMethodOf(toList.Method, _toList))
            source = toList.Arguments[0];

        Expression call;
        if (source is MethodCallExpression select && IsGenericMethodOf(select.Method, _select) && Unquote(select.Arguments[1]) is LambdaExpression selector
            && (arguments.Count == 1 || Unquote(arguments[1]) is LambdaExpression))
        {
            var element = selector.Parameters[0];
            var method = node.Method.GetGenericMethodDefinition().MakeGenericMethod(element.Type);

            if (arguments.Count == 2)
            {
                var predicate = (LambdaExpression)Unquote(arguments[1]);
                var body = new ReplaceParameterVisitor(predicate.Parameters[0], selector.Body).Visit(predicate.Body);
                call = Expression.Call(method, select.Arguments[0], Expression.Lambda(Visit(body), element));
            }
            else
            {
                call = Expression.Call(method, select.Arguments[0]);
            }
        }
        else
        {
            call = arguments.Count == 2
                ? Expression.Call(node.Method, source, arguments[1])
                : Expression.Call(node.Method, source);
        }

        return hasGuard ? And(condition, call) : call;
    }

    /// <summary>A comparison whose operands were visited but not hoisted; an operand lifted on the way still needs its types aligned.</summary>
    private static BinaryExpression Rebuild(BinaryExpression node, Expression left, Expression right)
        => left == node.Left && right == node.Right ? node : MakeComparison(node, left, right);

    /// <summary>
    /// Rebuilds the comparison of <paramref name="template"/> with new operands. Pushing a guard through a
    /// member access lifts a value-type member to its nullable form, so the lift is undone (or mirrored on the
    /// other operand) to keep both operand types equal.
    /// </summary>
    private static BinaryExpression MakeComparison(BinaryExpression template, Expression left, Expression right)
    {
        if (left.Type != right.Type)
        {
            if (left is UnaryExpression { NodeType: ExpressionType.Convert } leftLift && leftLift.Operand.Type == right.Type)
                left = leftLift.Operand;
            else if (right is UnaryExpression { NodeType: ExpressionType.Convert } rightLift && rightLift.Operand.Type == left.Type)
                right = rightLift.Operand;
            else if (Nullable.GetUnderlyingType(left.Type) == right.Type)
                right = Expression.Convert(right, left.Type);
            else if (Nullable.GetUnderlyingType(right.Type) == left.Type)
                left = Expression.Convert(left, right.Type);
        }

        return Expression.MakeBinary(template.NodeType, left, right, template.IsLiftedToNull, template.Method);
    }

    /// <summary>
    /// Recognises a null guard — <c>g ? e : null</c> or <c>!g ? null : e</c> — possibly wrapped in conversions,
    /// and returns the condition under which the value is evaluated and the value with the conversions re-applied.
    /// </summary>
    private static bool TryGetGuard(Expression expression, out Expression condition, out Expression value)
    {
        condition = value = null!;

        var conversions = new List<UnaryExpression>();
        var current = expression;
        while (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
        {
            conversions.Add(conversion);
            current = conversion.Operand;
        }

        if (current is not ConditionalExpression conditional)
            return false;

        if (IsNull(conditional.IfFalse) && !IsNull(conditional.IfTrue))
        {
            condition = conditional.Test;
            value = conditional.IfTrue;
        }
        else if (IsNull(conditional.IfTrue) && !IsNull(conditional.IfFalse))
        {
            condition = Negate(conditional.Test);
            value = conditional.IfFalse;
        }
        else
        {
            return false;
        }

        for (var i = conversions.Count - 1; i >= 0; i--)
            value = conversions[i].Update(value);

        return true;
    }

    /// <summary>Builds <c>condition ? value : null</c>; a value that is itself a guard is flattened into one guard.</summary>
    private static Expression MakeGuard(Expression condition, Expression value)
    {
        // (g1 ? (g2 ? e : null) : null)  =>  (g1 && g2 ? e : null)
        if (TryGetGuard(value, out var innerCondition, out var innerValue))
            return MakeGuard(And(condition, innerCondition), innerValue);

        var lifted = NullableExpressionHelper.LiftToNullable(value);
        return Expression.Condition(condition, lifted, Expression.Constant(null, lifted.Type));
    }

    /// <summary>
    /// <c>left &amp;&amp; right</c> without repeating a null check: the same guard reaches a comparison both from
    /// the projection and from safe navigation on the view side. Only plain null checks on a member path of a
    /// parameter (<c>p.A.B != null</c>, <c>p.A.B == null</c> against a literal <c>null</c>) are de-duplicated, keyed by
    /// the parameter instance and the path; any other term is kept, since RQL hosts filter values in closures
    /// that do not print their value (<c>eq(id,1)</c> and <c>eq(id,2)</c> print alike).
    /// </summary>
    private static Expression And(Expression left, Expression right)
    {
        var terms = new List<Expression>();
        var nullChecks = new HashSet<(ExpressionType, ParameterExpression, string)>();
        Collect(left);
        Collect(right);

        return terms.Skip(1).Aggregate(terms[0], Expression.AndAlso);

        void Collect(Expression expression)
        {
            if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso } and)
            {
                Collect(and.Left);
                Collect(and.Right);
            }
            else if (!TryGetNullCheckKey(expression, out var key) || nullChecks.Add(key))
            {
                terms.Add(expression);
            }
        }
    }

    /// <summary>Identifies <c>p.A.B == null</c> / <c>!= null</c> by operator, parameter instance and member path.</summary>
    private static bool TryGetNullCheckKey(Expression expression, out (ExpressionType, ParameterExpression, string) key)
    {
        key = default;
        if (expression is not BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual, Right: ConstantExpression { Value: null } } check)
            return false;

        var path = new Stack<string>();
        var current = check.Left;
        while (current is MemberExpression member)
        {
            path.Push(member.Member.Name);
            current = member.Expression!;
        }

        if (current is not ParameterExpression parameter)
            return false;

        key = (check.NodeType, parameter, string.Join('.', path));
        return true;
    }

    /// <summary>Negates a guard condition, keeping the result free of <c>Not</c> where the shape allows it.</summary>
    private static Expression Negate(Expression condition) => condition switch
    {
        BinaryExpression { NodeType: ExpressionType.Equal } equal when IsNull(equal.Right)
            => Expression.MakeBinary(ExpressionType.NotEqual, equal.Left, equal.Right, equal.IsLiftedToNull, equal.Method),
        BinaryExpression { NodeType: ExpressionType.NotEqual } notEqual when IsNull(notEqual.Right)
            => Expression.MakeBinary(ExpressionType.Equal, notEqual.Left, notEqual.Right, notEqual.IsLiftedToNull, notEqual.Method),
        BinaryExpression { NodeType: ExpressionType.AndAlso } and => Expression.OrElse(Negate(and.Left), Negate(and.Right)),
        BinaryExpression { NodeType: ExpressionType.OrElse } or => Expression.AndAlso(Negate(or.Left), Negate(or.Right)),
        UnaryExpression { NodeType: ExpressionType.Not } not => not.Operand,
        _ => Expression.Not(condition)
    };

    /// <summary>A <c>null</c> constant, including the hosted constants RQL builds for <c>eq(path,null)</c>.</summary>
    private static bool IsNull(Expression expression) => expression switch
    {
        ConstantExpression { Value: null } => true,
        DefaultExpression { Type.IsValueType: false } => true,
        MemberExpression { Expression: ConstantExpression { Value: IHostedConstant hosted } } => hosted.GetValue() is null,
        _ => false
    };

    /// <summary>Object initialisers, <c>new</c> and <c>ToList()</c> results never evaluate to <c>null</c>.</summary>
    private static bool IsNeverNull(Expression expression) => expression switch
    {
        MemberInitExpression or NewExpression => true,
        MethodCallExpression call when IsGenericMethodOf(call.Method, _toList) => true,
        UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion => IsNeverNull(conversion.Operand),
        _ => false
    };

    /// <summary>Non-nullable value types, non-null constants (including RQL's hosted constants) and object initialisers.</summary>
    private static bool IsKnownNonNull(Expression expression) => expression switch
    {
        { Type: var type } when type.IsValueType && Nullable.GetUnderlyingType(type) is null => true,
        ConstantExpression constant => constant.Value != null,
        MemberExpression { Expression: ConstantExpression { Value: IHostedConstant hosted } } => hosted.GetValue() != null,
        UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion => IsKnownNonNull(conversion.Operand),
        _ => IsNeverNull(expression)
    };

    private static bool SameMember(MemberInfo left, MemberInfo right)
        => left.Name == right.Name && left.DeclaringType == right.DeclaringType;

    private static bool IsGenericMethodOf(MethodInfo method, MethodInfo definition)
        => method.IsGenericMethod && method.GetGenericMethodDefinition() == definition;

    private static Expression Unquote(Expression expression)
        => expression is UnaryExpression { NodeType: ExpressionType.Quote } quote ? quote.Operand : expression;
}
