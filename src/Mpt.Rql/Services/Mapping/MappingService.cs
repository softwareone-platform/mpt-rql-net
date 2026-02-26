using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Mapping;

internal class MappingService<TStorage, TView>(IQueryContext<TView> queryContext, IEntityMapCache mapCache, IRqlSettings settings) : IMappingService<TStorage, TView>
{
    private readonly bool _useSafeNavigation = settings.Mapping.Navigation == NavigationStrategy.Safe;

    public IQueryable<TView> Apply(IQueryable<TStorage> query)
    {
        var param = Expression.Parameter(typeof(TStorage));

        var initExpression = MakeInitExpression(param, queryContext.Graph, typeof(TStorage), typeof(TView));
        var selector = Expression.Lambda(initExpression!, param);
        return query.Select((Expression<Func<TStorage, TView>>)selector);
    }

    private MemberInitExpression MakeInitExpression(Expression param, IRqlNode rqlNode, Type typeFrom, Type typeTo)
    {
        var typeMap = mapCache.Get(typeFrom, typeTo);
        return MakeInitExpression(param, rqlNode, typeTo, typeMap);
    }

    private MemberInitExpression MakeInitExpression(Expression param, IRqlNode rqlNode, Type typeTo, IReadOnlyDictionary<string, RqlMapEntry> typeMap)
    {
        var bindings = new List<MemberBinding>(queryContext.Graph.Count);

        foreach (var node in rqlNode.Children.Where(t => t.IsIncluded))
        {
            if (typeMap.TryGetValue(node.Property.Property.Name, out var map))
            {
                // check if the target property is writable before attempting to bind
                if (!node.Property.Property.CanWrite)
                    continue;

                var fromExpression = MakeBindExpression(param, node, map);
                fromExpression = TryMakeConditionalBindExpression(param, node, fromExpression, map);
                bindings.Add(Expression.Bind(node.Property.Property, fromExpression));
            }
        }

        return Expression.MemberInit(Expression.New(typeTo.GetConstructor(Type.EmptyTypes)!), bindings);
    }

    private Expression TryMakeConditionalBindExpression(Expression param, IRqlNode rqlNode, Expression defaultExpression, RqlMapEntry parentEntry)
    {
        if (parentEntry.Conditions == null || parentEntry.Conditions.Count == 0)
            return defaultExpression;

        Expression conditionalExpr = defaultExpression;

        for (int i = parentEntry.Conditions.Count - 1; i >= 0; i--)
        {
            var condition = parentEntry.Conditions[i];

            var replaceParamVisitor = new ReplaceParameterVisitor(condition.If.Parameters[0], param);
            var ifExpression = replaceParamVisitor.Visit(condition.If.Body);

            conditionalExpr = Expression.Condition(ifExpression, MakeBindExpression(param, rqlNode, condition.Entry), conditionalExpr);
        }

        return conditionalExpr;
    }

    private Expression MakeBindExpression(Expression param, IRqlNode node, RqlMapEntry map)
    {
        var targetType = node.Property.Property.PropertyType;

        LambdaExpression sourceExpression;
        var hint = ExpressionFactoryHint.None;

        if (map.FactoryType != null)
        {
            var factory = queryContext.ExternalServices.GetService(map.FactoryType) as IRqlMappingExpressionFactory
                ?? throw new RqlMappingException($"Expression factory of type {map.FactoryType.Name} not found in dependency injection container. Ensure it is registered.");
            sourceExpression = factory.GetStorageExpressionLambda();
            hint = factory.Hint;
        }
        else
        {
            sourceExpression = map.SourceExpression!;
        }

        var replaceParamVisitor = new ReplaceParameterVisitor(sourceExpression.Parameters[0], param);
        var fromExpression = replaceParamVisitor.Visit(ExpressionHelper.UnwrapCastExpression(sourceExpression.Body));

        fromExpression = hint switch
        {
            ExpressionFactoryHint.TakeFirst => MakeTakeFirstAccess(fromExpression, node, map),
            _ => node.Property.Type switch
            {
                RqlPropertyType.Reference => MakeReferenceInit(fromExpression, node, map),
                RqlPropertyType.Collection => MakeCollectionInit(fromExpression, node, map, f => f.GetToList()),
                _ => MakeDirectPropertyAccess(fromExpression)
            }
        };

        if (!IsTypeCompatible(targetType, fromExpression.Type))
            throw new NotSupportedException($"Cannot map property '{node.Property.Property.Name}' of type {node.Property.Property.DeclaringType!.Name}. Type mismatch.");

        if (targetType != fromExpression.Type)
            fromExpression = Expression.Convert(fromExpression, targetType);

        return fromExpression;
    }

    private Expression MakeTakeFirstAccess(Expression fromExpression, IRqlNode node, RqlMapEntry map)
    {
        if (!fromExpression.Type.IsGenericType || fromExpression.Type.GenericTypeArguments.Length == 0)
            throw new RqlMappingException($"Expression factory with TakeFirst hint for property '{node.Property.Property.Name}' must return a collection expression.");

        return MakeCollectionInitCore(fromExpression, node, map, f => f.GetFirstOrDefault(), applyToPrimitives: true);
    }

    private Expression MakeReferenceInit(Expression fromExpression, IRqlNode node, RqlMapEntry map)
    {
        if (!map.IsDynamic)
            return MakeDirectPropertyAccess(fromExpression);

        var innerMap = GetInnerMapFromEntry(fromExpression.Type, map);
        var subInit = MakeInitExpression(fromExpression, node, map.TargetType, innerMap);

        // When safe navigation is enabled or property is nullable, add null check
        // This handles deserialized data where non-nullable reference types may be null
        if (_useSafeNavigation || node.Property.IsNullable)
        {
            return Expression.Condition(
                Expression.NotEqual(fromExpression, Expression.Constant(null, fromExpression.Type)),
                subInit,
                Expression.Constant(null, subInit.Type));
        }

        return subInit;
    }

    private Expression MakeDirectPropertyAccess(Expression fromExpression)
        => _useSafeNavigation ? ApplyNullPropagation(fromExpression) : fromExpression;

    private Expression MakeCollectionInit(Expression fromExpression, IRqlNode node, RqlMapEntry map, Func<IProjectionFunctions, MethodInfo> finalFunctionSelector)
    {
        if (!map.IsDynamic)
            return MakeDirectPropertyAccess(fromExpression);

        // Temporarily only support List
        if (!typeof(IList).IsAssignableFrom(node.Property.Property.PropertyType))
            throw new NotSupportedException($"Cannot map property '{node.Property.Property.Name}' of type {node.Property.Property.DeclaringType!.Name}. Rql temporarily support only list coollections.");

        return MakeCollectionInitCore(fromExpression, node, map, finalFunctionSelector);
    }

    private Expression MakeCollectionInitCore(Expression fromExpression, IRqlNode node, RqlMapEntry map, Func<IProjectionFunctions, MethodInfo> finalFunctionSelector, bool applyToPrimitives = false)
    {
        var srcItemType = fromExpression.Type.GenericTypeArguments[0];

        Expression finalExpression;

        if (!TypeHelper.IsUserComplexType(srcItemType))
        {
            finalExpression = fromExpression;
            if (applyToPrimitives)
            {
                var primitiveFunctions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<>).MakeGenericType(srcItemType))!;
                finalExpression = Expression.Call(null, finalFunctionSelector(primitiveFunctions), fromExpression);
            }
        }
        else
        {
            var innerMap = GetInnerMapFromEntry(srcItemType, map);
            var innerParam = Expression.Parameter(srcItemType);
            var subInit = MakeInitExpression(innerParam, node, map.TargetType, innerMap);
            var selectLambda = Expression.Lambda(subInit, innerParam);
            var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<,>).MakeGenericType(srcItemType, map.TargetType))!;
            var selectCall = Expression.Call(null, functions.GetSelect(), fromExpression, selectLambda);
            finalExpression = Expression.Call(null, finalFunctionSelector(functions), selectCall);
        }

        // Add null check for the collection if safe navigation is enabled
        if (_useSafeNavigation)
        {
            return Expression.Condition(
                Expression.NotEqual(fromExpression, Expression.Constant(null, fromExpression.Type)),
                finalExpression,
                Expression.Constant(null, finalExpression.Type));
        }

        return finalExpression;
    }

    private IReadOnlyDictionary<string, RqlMapEntry> GetInnerMapFromEntry(Type typeFrom, RqlMapEntry map)
        => map.InlineMap ?? mapCache.Get(typeFrom, map.TargetType);

    /// <summary>
    /// Applies null propagation to a member access expression chain.
    /// Converts expression like "a.b.c" to "a == null ? null : (a.b == null ? null : a.b.c)"
    /// This is useful for deserialized data where non-nullable properties may not be set.
    /// </summary>
    private static Expression ApplyNullPropagation(Expression expression)
    {
        var memberAccesses = ExtractMemberAccessChain(expression);

        // If there are no member accesses or only one (no intermediate nulls possible), return as-is
        if (memberAccesses.Count <= 1)
            return expression;

        // Build conditional expressions from the member access chain
        return BuildConditionalExpression(memberAccesses, 0);
    }

    /// <summary>
    /// Extracts the chain of member accesses from an expression.
    /// For example, "a.b.c" returns [a, a.b, a.b.c]
    /// </summary>
    private static List<Expression> ExtractMemberAccessChain(Expression expression)
    {
        var chain = new List<Expression>();
        var current = expression;

        // Walk up the expression tree collecting member accesses
        while (current is MemberExpression memberExpr)
        {
            chain.Insert(0, current);
            current = memberExpr.Expression;
        }

        // Add the root if it's a reference type that could be null
        if (current != null && !current.Type.IsValueType && chain.Count > 0)
        {
            chain.Insert(0, current);
        }

        return chain;
    }

    /// <summary>
    /// Builds a conditional expression that checks for null at each level.
    /// </summary>
    private static Expression BuildConditionalExpression(List<Expression> memberAccess, int index)
    {
        if (index == memberAccess.Count - 1)
        {
            return memberAccess[index];
        }

        var currentAccess = memberAccess[index];
        var nextAccess = BuildConditionalExpression(memberAccess, index + 1);
        var nextAccessType = nextAccess.Type;

        // Skip null check for value types that aren't nullable
        if (!currentAccess.Type.IsValueType || Nullable.GetUnderlyingType(currentAccess.Type) != null)
        {
            if (nextAccessType.IsValueType && Nullable.GetUnderlyingType(nextAccessType) == null)
            {
                // This is a non-nullable value type, make it nullable for the comparison
                nextAccessType = typeof(Nullable<>).MakeGenericType(nextAccessType);
                nextAccess = Expression.Convert(nextAccess, nextAccessType);
            }

            return Expression.Condition(
                Expression.Equal(currentAccess, Expression.Constant(null, currentAccess.Type)),
                Expression.Constant(null, nextAccessType),
                nextAccess);
        }

        return nextAccess;
    }

    /// <summary>
    /// Checks if sourceType can be assigned to targetType, considering nullable value type conversions.
    /// Handles cases where null propagation converts non-nullable types to nullable types.
    /// </summary>
    private static bool IsTypeCompatible(Type targetType, Type sourceType)
    {
        // Direct assignability check
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        // Check if source is nullable and target is the underlying type
        var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType);
        if (sourceUnderlyingType != null)
        {
            // Source is Nullable<T>, check if target is T or compatible with T
            if (targetType == sourceUnderlyingType || targetType.IsAssignableFrom(sourceUnderlyingType))
                return true;
        }

        // Check if target is nullable and source is the underlying type
        var targetUnderlyingType = Nullable.GetUnderlyingType(targetType);
        if (targetUnderlyingType != null)
        {
            // Target is Nullable<T>, check if source is T or compatible with T
            if (sourceType == targetUnderlyingType || targetUnderlyingType.IsAssignableFrom(sourceType))
                return true;
        }

        return false;
    }
}
