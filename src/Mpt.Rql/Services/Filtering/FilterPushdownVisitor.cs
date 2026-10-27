using System.Linq.Expressions;
using System.Reflection;
using static Mpt.Rql.Core.CollectionQueryHelper;

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
    private readonly ParameterExpression _view;
    private readonly Expression _projection;
    private readonly bool _removeRedundantNullChecks;

    private FilterPushdownVisitor(
        ParameterExpression view,
        Expression projection,
        bool removeRedundantNullChecks)
    {
        _view = view;
        _projection = projection;
        _removeRedundantNullChecks = removeRedundantNullChecks;
    }

    /// <summary>
    /// Filters the source of a projected query: <c>source.Select(projection)</c> becomes
    /// <c>source.Where(filter).Select(projection)</c>, with the filter rewritten onto the source.
    /// A query that is not a projection is filtered as it is.
    /// </summary>
    public static IQueryable<TView> Apply<TView>(
        IQueryable<TView> query,
        Expression<Func<TView, bool>> filter,
        bool removeRedundantNullChecks = true)
    {
        if (query.Expression is not MethodCallExpression
            {
                Arguments:
                [
                    var source,
                    UnaryExpression
                    {
                        Operand: LambdaExpression
                        {
                            Parameters.Count: 1
                        } projection
                    }
                ]
            } select ||
            select.Method.DeclaringType != typeof(Queryable) ||
            select.Method.Name != nameof(Queryable.Select))
        {
            return query.Where(filter);
        }

        var sourceFilter = ToSourceFilter(
            filter,
            projection,
            removeRedundantNullChecks);

        var where = Expression.Call(
            QueryableWhere.MakeGenericMethod(projection.Parameters[0].Type),
            source,
            Expression.Quote(sourceFilter));

        return query.Provider.CreateQuery<TView>(
            select.Update(null, [where, select.Arguments[1]]));
    }

    /// <summary>
    /// Rewrites a filter over the view into a filter over the parameter of the projection.
    /// </summary>
    public static LambdaExpression ToSourceFilter(
        LambdaExpression filter,
        LambdaExpression projection,
        bool removeRedundantNullChecks = true)
    {
        var visitor = new FilterPushdownVisitor(
            filter.Parameters[0],
            projection.Body,
            removeRedundantNullChecks);

        var body = visitor.Visit(filter.Body);

        return Expression.Lambda(body, projection.Parameters[0]);
    }

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _view ? _projection : node;

    protected override Expression VisitMember(MemberExpression node)
    {
        var source = Visit(node.Expression);

        if (source != null &&
            Bind(source, node.Member) is { } bound &&
            bound.Type == node.Type)
        {
            return Visit(bound);
        }

        return node.Update(source);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var test = Visit(node.Test);

        if (test is ConstantExpression { Value: bool condition })
            return Visit(condition ? node.IfTrue : node.IfFalse);

        return MakeConditional(
            test,
            Visit(node.IfTrue),
            Visit(node.IfFalse));
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (IsGuardedQuantifier(node))
        {
            var comparison = (BinaryExpression)node.Left;
            var collection = ComparedWithNull(comparison)!;
            var quantifier = (MethodCallExpression)node.Right;

            var source = Visit(collection);

            var arguments = quantifier.Arguments
                .Skip(1)
                .Select(argument => Visit(argument))
                .ToArray();

            return RewriteGuardedQuantifier(source, quantifier, arguments);
        }

        var visited = (BinaryExpression)base.VisitBinary(node);

        if (visited.NodeType is ExpressionType.Equal or ExpressionType.NotEqual &&
            ComparedWithNull(visited) is { } operand &&
            IsNull(operand) is { } isNull)
        {
            return visited.NodeType == ExpressionType.Equal
                ? isNull
                : Negate(isNull);
        }

        if (visited.NodeType == ExpressionType.AndAlso)
            return MakeAndAlso(visited.Left, visited.Right);

        return visited;
    }

    private Expression RewriteGuardedQuantifier(
        Expression source,
        MethodCallExpression quantifier,
        IReadOnlyList<Expression> arguments)
    {
        if (IsNullValue(source))
            return Expression.Constant(false);

        if (source is ConditionalExpression conditional)
        {
            var test = Visit(conditional.Test);

            if (test is ConstantExpression { Value: bool condition })
            {
                return RewriteGuardedQuantifier(
                    Visit(condition ? conditional.IfTrue : conditional.IfFalse),
                    quantifier,
                    arguments);
            }

            return MakeConditional(
                test,
                RewriteGuardedQuantifier(
                    Visit(conditional.IfTrue),
                    quantifier,
                    arguments),
                RewriteGuardedQuantifier(
                    Visit(conditional.IfFalse),
                    quantifier,
                    arguments));
        }

        var call = quantifier.Update(null, [source, .. arguments]);
        var rewritten = RewriteQuantifier(call);

        var isNull = IsNull(source) ??
                     Expression.Equal(source, Expression.Constant(null, source.Type));

        return MakeAndAlso(Negate(isNull), rewritten);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var visited = (MethodCallExpression)base.VisitMethodCall(node);

        if (IsQuantifier(visited))
            return RewriteQuantifier(visited);

        return visited;
    }

    private Expression RewriteQuantifier(MethodCallExpression quantifier)
    {
        var source = quantifier.Arguments[0];

        while (source is MethodCallExpression materialization &&
               IsMaterialization(materialization))
        {
            source = materialization.Arguments[0];
        }
        
        if (source is ConditionalExpression conditional)
        {
            return MakeConditional(
                conditional.Test,
                RewriteQuantifierBranch(quantifier, conditional.IfTrue),
                RewriteQuantifierBranch(quantifier, conditional.IfFalse));
        }

        var arguments = quantifier.Arguments.ToArray();
        arguments[0] = source;

        var updated = quantifier.Update(null, arguments);

        if (source is not MethodCallExpression
            {
                Arguments:
                [
                    var originalSource,
                    LambdaExpression { Parameters.Count: 1 } selector
                ]
            } select ||
            select.Method.DeclaringType != typeof(Enumerable) ||
            select.Method.Name != nameof(Enumerable.Select))
        {
            return updated;
        }

        var method = quantifier.Method
            .GetGenericMethodDefinition()
            .MakeGenericMethod(selector.Parameters[0].Type);

        MethodCallExpression rewritten;

        if (quantifier.Arguments.Count == 1)
        {
            rewritten = Expression.Call(method, originalSource);
        }
        else
        {
            if (quantifier.Arguments[1] is not LambdaExpression
                {
                    Parameters.Count: 1
                } predicate)
            {
                return updated;
            }

            var predicateVisitor = new FilterPushdownVisitor(
                predicate.Parameters[0],
                selector.Body,
                _removeRedundantNullChecks);

            var predicateBody = predicateVisitor.Visit(predicate.Body);

            var sourcePredicate = Expression.Lambda(
                predicateBody,
                selector.Parameters[0]);

            rewritten = Expression.Call(
                method,
                originalSource,
                sourcePredicate);
        }

        return RewriteQuantifier(rewritten);
    }

    private Expression RewriteQuantifierBranch(
        MethodCallExpression quantifier,
        Expression source)
    {
        if (IsNullValue(source))
            return Expression.Constant(false);

        var arguments = quantifier.Arguments.ToArray();
        arguments[0] = source;

        return RewriteQuantifier(quantifier.Update(null, arguments));
    }
    
    private static bool IsMaterialization(MethodCallExpression call)
    {
        return call.Method.DeclaringType == typeof(Enumerable) &&
            call.Method.Name is nameof(Enumerable.ToList) or nameof(Enumerable.ToArray) &&
            call.Arguments.Count == 1;
    }

    /// <summary>
    /// Reads a member of an object the projection builds; null for any other source.
    /// </summary>
    private Expression? Bind(Expression source, MemberInfo member)
    {
        if (source is MemberInitExpression initializer)
        {
            foreach (var binding in initializer.Bindings)
            {
                if (binding is MemberAssignment assignment &&
                    IsSameMember(assignment.Member, member))
                {
                    return assignment.Expression;
                }
            }

            return null;
        }

        if (source is not ConditionalExpression conditional)
            return null;

        var nullWhenFalse = IsNullValue(conditional.IfFalse);
        var nullWhenTrue = IsNullValue(conditional.IfTrue);

        if (!nullWhenFalse && !nullWhenTrue)
            return null;

        var presentBranch = nullWhenFalse
            ? conditional.IfTrue
            : conditional.IfFalse;

        var bound = Bind(presentBranch, member);

        if (bound is null)
            return null;

        if (_removeRedundantNullChecks &&
            nullWhenFalse &&
            conditional.Test is BinaryExpression
            {
                NodeType: ExpressionType.NotEqual,
                Right: ConstantExpression { Value: null }
            } test &&
            IsMemberChainOf(bound, test.Left))
        {
            return bound;
        }

        if (bound.Type.IsValueType &&
            Nullable.GetUnderlyingType(bound.Type) is null)
        {
            return null;
        }

        var missingValue = Expression.Constant(null, bound.Type);

        return nullWhenFalse
            ? MakeConditional(conditional.Test, bound, missingValue)
            : MakeConditional(conditional.Test, missingValue, bound);
    }
    
    /// <summary>
    /// The test for <c>operand == null</c> when the projection builds the operand: a new object or list is never null,
    /// and an object behind a null check is null when the check fails. Null for any other operand.
    /// </summary>
    private static Expression? IsNull(Expression operand)
    {
        if (IsNullValue(operand))
            return Expression.Constant(true);

        if (operand is MemberInitExpression ||
            operand is MethodCallExpression call && IsMaterialization(call))
        {
            return Expression.Constant(false);
        }

        if (operand is ConditionalExpression conditional)
        {
            var trueBranchIsNull = IsNull(conditional.IfTrue) ??
                MakeNullComparison(conditional.IfTrue);

            var falseBranchIsNull = IsNull(conditional.IfFalse) ??
                MakeNullComparison(conditional.IfFalse);

            return MakeConditional(
                conditional.Test,
                trueBranchIsNull,
                falseBranchIsNull);
        }

        return null;
    }

    private static Expression MakeNullComparison(Expression expression)
    {
        if (expression.Type.IsValueType &&
            Nullable.GetUnderlyingType(expression.Type) is null)
        {
            return Expression.Constant(false);
        }

        return Expression.Equal(
            expression,
            Expression.Constant(null, expression.Type));
    }

    private static bool IsNullValue(Expression expression)
    {
        return expression is ConstantExpression { Value: null } ||
            expression is DefaultExpression &&
            (!expression.Type.IsValueType ||
             Nullable.GetUnderlyingType(expression.Type) is not null);
    }

    private static Expression Negate(Expression condition)
    {
        return condition switch
        {
            ConstantExpression { Value: bool value } =>
                Expression.Constant(!value),

            BinaryExpression { NodeType: ExpressionType.Equal } equal =>
                Expression.NotEqual(equal.Left, equal.Right),

            BinaryExpression { NodeType: ExpressionType.NotEqual } notEqual =>
                Expression.Equal(notEqual.Left, notEqual.Right),

            UnaryExpression { NodeType: ExpressionType.Not } not =>
                not.Operand,

            _ => Expression.Not(condition)
        };
    }

    private static Expression MakeAndAlso(Expression left, Expression right)
    {
        if (left is ConstantExpression { Value: bool leftValue })
            return leftValue ? right : Expression.Constant(false);

        if (right is ConstantExpression { Value: true })
            return left;

        return Expression.AndAlso(left, right);
    }

    private static Expression MakeConditional(
        Expression test,
        Expression ifTrue,
        Expression ifFalse)
    {
        if (test is ConstantExpression { Value: bool condition })
            return condition ? ifTrue : ifFalse;

        if (ifTrue is ConstantExpression { Value: true } &&
            ifFalse is ConstantExpression { Value: false })
        {
            return test;
        }

        if (ifTrue is ConstantExpression { Value: false } &&
            ifFalse is ConstantExpression { Value: true })
        {
            return Negate(test);
        }

        return Expression.Condition(test, ifTrue, ifFalse);
    }

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
}
