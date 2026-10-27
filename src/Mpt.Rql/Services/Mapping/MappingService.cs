using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using System.Collections;
using Mpt.Rql.Core.Expressions;
using System.Linq.Expressions;
using System.Reflection;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Settings;

namespace Mpt.Rql.Services.Mapping;

internal class MappingService<TStorage, TView>(
    IQueryContext<TView> queryContext,
    IEntityMapCache mapCache,
    IRqlSettings settings,
    RqlTransformOptions options)
    : IMappingService<TStorage, TView>
{
    private readonly bool _useSafeNavigation =
        settings.Mapping.Navigation == NavigationStrategy.Safe;

    private readonly ParameterExpression _sourceParameter =
        Expression.Parameter(typeof(TStorage), "source");
    public IQueryable<TView> Apply(IQueryable<TStorage> query)
    {
        var initExpression = MakeInitExpression(
            _sourceParameter,
            queryContext.Graph,
            typeof(TStorage),
            typeof(TView));

        var selector = Expression.Lambda<Func<TStorage, TView>>(
            initExpression,
            _sourceParameter);

        return query.Select(selector);
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

            CollectionScopeMappingValidator.Validate(
                ifExpression,
                _sourceParameter,
                options.CollectionScopes,
                $"{rqlNode.Property.Property.Name} (condition)");
            
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

        CollectionScopeMappingValidator.Validate(
            fromExpression,
            _sourceParameter,
            options.CollectionScopes,
            node.Property.Property.Name,
            allowDirectCollection:
            node.Property.Type == RqlPropertyType.Collection ||
            hint == ExpressionFactoryHint.TakeFirst,
            allowReferenceTraversal:
            map.IsDynamic &&
            node.Property.Type == RqlPropertyType.Reference &&
            hint == ExpressionFactoryHint.None);
        
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

    private Expression MakeTakeFirstAccess(
        Expression fromExpression,
        IRqlNode node,
        RqlMapEntry map)
    {
        if (fromExpression.Type == typeof(string) ||
            CollectionQueryHelper
                .GetEnumerableElementTypes(fromExpression.Type)
                .Length != 1)
        {
            throw new RqlMappingException(
                $"TakeFirst requires a collection expression for property " +
                $"'{node.Property.Property.Name}'. " +
                $"Expression type: '{fromExpression.Type}'.");
        }

        return MakeCollectionInitCore(
            fromExpression,
            node,
            map,
            functions => functions.GetFirstOrDefault(),
            applyToPrimitives: true);
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

    private Expression MakeCollectionInit(
        Expression fromExpression,
        IRqlNode node,
        RqlMapEntry map,
        Func<IProjectionFunctions, MethodInfo> finalFunctionSelector)
    {
        if (!map.IsDynamic)
        {
            var scopedSource = ApplyCollectionScopes(fromExpression);

            if (ReferenceEquals(scopedSource, fromExpression))
                return MakeDirectPropertyAccess(fromExpression);

            var elementType = GetCollectionElementType(fromExpression.Type);

            var materialized = MaterializeCollection(
                scopedSource,
                elementType,
                node.Property.Property.PropertyType);

            return GuardCollectionSource(fromExpression, materialized);
        }
        
        if (!typeof(IList).IsAssignableFrom(
                node.Property.Property.PropertyType))
        {
            throw new NotSupportedException($"Cannot map property '{node.Property.Property.Name}' of type {node.Property.Property.DeclaringType!.Name}. Rql temporarily support only list coollections.");
        }
        
        return MakeCollectionInitCore(
            fromExpression,
            node,
            map,
            finalFunctionSelector);
    }

    private Expression MakeCollectionInitCore(
        Expression fromExpression,
        IRqlNode node,
        RqlMapEntry map,
        Func<IProjectionFunctions, MethodInfo> finalFunctionSelector,
        bool applyToPrimitives = false)
    {
        var sourceElementType = GetCollectionElementType(fromExpression.Type);
        var scopedSource = ApplyCollectionScopes(fromExpression);

        if (!applyToPrimitives &&
            !TypeHelper.IsUserComplexType(sourceElementType) &&
            ReferenceEquals(scopedSource, fromExpression))
        {
            return GuardCollectionSource(fromExpression, fromExpression);
        }
        
        Expression projectedSource;
        Type resultElementType;

        if (!TypeHelper.IsUserComplexType(sourceElementType))
        {
            projectedSource = scopedSource;
            resultElementType = sourceElementType;
        }
        else
        {
            var innerMap = GetInnerMapFromEntry(sourceElementType, map);
            var innerParameter = Expression.Parameter(sourceElementType);

            var initializer = MakeInitExpression(
                innerParameter,
                node,
                map.TargetType,
                innerMap);

            var selector = Expression.Lambda(initializer, innerParameter);

            var functions = (IProjectionFunctions)Activator.CreateInstance(
                typeof(ProjectionFunctions<,>).MakeGenericType(
                    sourceElementType,
                    map.TargetType))!;

            projectedSource = Expression.Call(
                null,
                functions.GetSelect(),
                scopedSource,
                selector);

            resultElementType = map.TargetType;
        }

        Expression finalExpression;

        if (applyToPrimitives)
        {
            var functions = (IProjectionFunctions)Activator.CreateInstance(
                typeof(ProjectionFunctions<>).MakeGenericType(resultElementType))!;

            finalExpression = Expression.Call(
                null,
                finalFunctionSelector(functions),
                projectedSource);
        }
        else
        {
            finalExpression = MaterializeCollection(
                projectedSource,
                resultElementType,
                node.Property.Property.PropertyType);
        }

        return GuardCollectionSource(fromExpression, finalExpression);
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

        // Skip null check for value types that aren't nullable
        if (!currentAccess.Type.IsValueType || Nullable.GetUnderlyingType(currentAccess.Type) != null)
            return NullableExpressionHelper.NullGuard(currentAccess, nextAccess);

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
    
    private Expression ApplyCollectionScopes(Expression source)
    {
        return CollectionScopeExpressionBuilder.Apply(
            source,
            _sourceParameter,
            options.CollectionScopes);
    }

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType == typeof(string))
        {
            throw new NotSupportedException(
                "A string cannot be used as a collection source.");
        }

        var elementTypes =
            CollectionQueryHelper.GetEnumerableElementTypes(collectionType);

        if (elementTypes.Length != 1)
        {
            throw new NotSupportedException(
                $"Collection type '{collectionType.Name}' must expose one IEnumerable<T> element type.");
        }

        return elementTypes[0];
    }

    private static Expression MaterializeCollection(
        Expression source,
        Type elementType,
        Type targetType)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);

        if (targetType.IsAssignableFrom(listType))
        {
            var functions = (IProjectionFunctions)Activator.CreateInstance(
                typeof(ProjectionFunctions<>).MakeGenericType(elementType))!;

            return Expression.Call(
                null,
                functions.GetToList(),
                source);
        }

        if (targetType.IsSZArray &&
            targetType.GetElementType() == elementType)
        {
            var toArrayMethod =
                ((Func<IEnumerable<object>, object[]>)Enumerable.ToArray)
                .Method
                .GetGenericMethodDefinition()
                .MakeGenericMethod(elementType);

            return Expression.Call(null, toArrayMethod, source);
        }

        throw new NotSupportedException(
            $"Cannot materialize collection elements of type '{elementType.Name}' " +
            $"into '{targetType.Name}'.");
    }

    private Expression GuardCollectionSource(
        Expression originalSource,
        Expression result)
    {
        if (!_useSafeNavigation)
            return result;

        var guardedSource = ApplyNullPropagation(originalSource);

        if (guardedSource.Type.IsValueType &&
            Nullable.GetUnderlyingType(guardedSource.Type) is null)
        {
            return result;
        }

        return Expression.Condition(
            Expression.NotEqual(
                guardedSource,
                Expression.Constant(null, guardedSource.Type)),
            result,
            Expression.Default(result.Type));
    }
}
