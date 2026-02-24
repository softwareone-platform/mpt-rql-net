using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Mapping;

internal class MappingService<TStorage, TView> : IMappingService<TStorage, TView>
{
    private readonly IQueryContext<TView> _queryContext;
    private readonly IEntityMapCache _mapCache;

    // Scoped per Apply() call — safe because MappingService is registered as Scoped.
    private SharedExpressionCollector? _collector;

    // Open generic Queryable.Select<TSource,TResult> resolved once at class load.
    private static readonly MethodInfo _queryableSelectOpen =
        typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m =>
                m.Name == "Select" &&
                m.GetGenericArguments().Length == 2 &&
                m.GetParameters()[1].ParameterType   // Expression<Func<TSource, TResult>>
                    .GetGenericArguments()[0]          // Func<TSource, TResult>
                    .GetGenericArguments().Length == 2); // 2 type args = non-indexed overload

    // Closed MethodInfo cached once per unique (source, result) type pair.
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> _selectMethodCache = new();

    public MappingService(IQueryContext<TView> queryContext, IEntityMapCache mapCache)
    {
        _queryContext = queryContext;
        _mapCache = mapCache;
    }

    public IQueryable<TView> Apply(IQueryable<TStorage> query)
    {
        var param = Expression.Parameter(typeof(TStorage));
        _collector = new SharedExpressionCollector();

        var initExpression = MakeInitExpression(param, _queryContext.Graph, typeof(TStorage), typeof(TView));

        if (!_collector.HasEntries)
        {
            _collector = null;
            var selector = Expression.Lambda<Func<TStorage, TView>>(initExpression, param);
            return query.Select(selector);
        }

        var entries = _collector.Entries.ToArray();
        _collector = null;
        return ApplyTwoStage(query, param, initExpression, entries);
    }

    private IQueryable<TView> ApplyTwoStage(
        IQueryable<TStorage> query,
        ParameterExpression rootParam,
        MemberInitExpression stage2Body,
        LiftedEntry[] entries)
    {
        Type intermediateType = entries.Length switch
        {
            1 => typeof(IntermediateProjection<,>).MakeGenericType(typeof(TStorage), entries[0].Placeholder.Type),
            2 => typeof(IntermediateProjection<,,>).MakeGenericType(typeof(TStorage), entries[0].Placeholder.Type, entries[1].Placeholder.Type),
            3 => typeof(IntermediateProjection<,,,>).MakeGenericType(typeof(TStorage), entries[0].Placeholder.Type, entries[1].Placeholder.Type, entries[2].Placeholder.Type),
            4 => typeof(IntermediateProjection<,,,,>).MakeGenericType(typeof(TStorage), entries[0].Placeholder.Type, entries[1].Placeholder.Type, entries[2].Placeholder.Type, entries[3].Placeholder.Type),
            5 => typeof(IntermediateProjection<,,,,,>).MakeGenericType(typeof(TStorage), entries[0].Placeholder.Type, entries[1].Placeholder.Type, entries[2].Placeholder.Type, entries[3].Placeholder.Type, entries[4].Placeholder.Type),
            _ => throw new NotSupportedException(
                $"Two-stage projection supports at most 5 lifted sub-expressions per entity. Found {entries.Length}. " +
                $"Use inline maps or restructure the mapping to reduce the number of expensive sub-navigations.")
        };

        // Stage 1 lambda: (TStorage s) => new Intermediate { Root = s, Sub0 = entry0.Original, ... }
        string[] subPropNames = ["Sub0", "Sub1", "Sub2", "Sub3", "Sub4"];
        var rootProp = intermediateType.GetProperty("Root")!;

        var stage1Bindings = new List<MemberBinding>(entries.Length + 1);
        stage1Bindings.Add(Expression.Bind(rootProp, rootParam));
        for (int i = 0; i < entries.Length; i++)
            stage1Bindings.Add(Expression.Bind(intermediateType.GetProperty(subPropNames[i])!, entries[i].OriginalExpression));

        var stage1Init = Expression.MemberInit(Expression.New(intermediateType.GetConstructor(Type.EmptyTypes)!), stage1Bindings);
        var stage1Func = typeof(Func<,>).MakeGenericType(typeof(TStorage), intermediateType);
        var stage1Lambda = Expression.Lambda(stage1Func, stage1Init, rootParam);

        // Apply Stage 1: IQueryable<TStorage> → IQueryable<IntermediateType>
        var stage1Query = (IQueryable)GetQueryableSelectMethod(typeof(TStorage), intermediateType)
            .Invoke(null, [query, stage1Lambda])!;

        // Stage 2: rewrite stage2Body replacing rootParam → i.Root, placeholders → i.SubN
        var intermediateParam = Expression.Parameter(intermediateType, "i");
        var replacements = new Dictionary<Expression, Expression>(entries.Length + 1)
        {
            [rootParam] = Expression.Property(intermediateParam, rootProp)
        };
        for (int i = 0; i < entries.Length; i++)
            replacements[entries[i].Placeholder] = Expression.Property(intermediateParam, intermediateType.GetProperty(subPropNames[i])!);

        var rewriter = new MultiExpressionReplacer(replacements);
        var rewrittenBody = (MemberInitExpression)rewriter.Visit(stage2Body);

        var stage2Func = typeof(Func<,>).MakeGenericType(intermediateType, typeof(TView));
        var stage2Lambda = Expression.Lambda(stage2Func, rewrittenBody, intermediateParam);

        // Apply Stage 2: IQueryable<IntermediateType> → IQueryable<TView>
        return (IQueryable<TView>)GetQueryableSelectMethod(intermediateType, typeof(TView))
            .Invoke(null, [stage1Query, stage2Lambda])!;
    }

    private static MethodInfo GetQueryableSelectMethod(Type source, Type result)
        => _selectMethodCache.GetOrAdd((source, result),
            k => _queryableSelectOpen.MakeGenericMethod(k.Item1, k.Item2));

    private MemberInitExpression MakeInitExpression(Expression param, IRqlNode rqlNode, Type typeFrom, Type typeTo)
    {
        var typeMap = _mapCache.Get(typeFrom, typeTo);
        return MakeInitExpression(param, rqlNode, typeTo, typeMap);
    }

    private MemberInitExpression MakeInitExpression(Expression param, IRqlNode rqlNode, Type typeTo, IReadOnlyDictionary<string, RqlMapEntry> typeMap)
    {
        var bindings = new List<MemberBinding>(_queryContext.Graph.Count);

        foreach (var node in rqlNode.Children.Where(t => t.IsIncluded))
        {
            if (typeMap.TryGetValue(node.Property.Property.Name, out var map))
            {
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

        // Suspend lifting inside conditional branches — different branches may navigate different
        // source objects, so sharing a placeholder across branches would produce incorrect SQL.
        var savedCollector = _collector;
        _collector = null;
        try
        {
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
        finally
        {
            _collector = savedCollector;
        }
    }

    private Expression MakeBindExpression(Expression param, IRqlNode node, RqlMapEntry map)
    {
        var targetType = node.Property.Property.PropertyType;

        LambdaExpression sourceExpression;
        if (map.FactoryType != null)
        {
            var factory = _queryContext.ExternalServices.GetService(map.FactoryType) as IRqlMappingExpressionFactory
                ?? throw new RqlMappingException($"Expression factory of type {map.FactoryType.Name} not found in dependency injection container. Ensure it is registered.");
            sourceExpression = factory.GetStorageExpressionLambda();
        }
        else
        {
            sourceExpression = map.SourceExpression!;
        }

        var replaceParamVisitor = new ReplaceParameterVisitor(sourceExpression.Parameters[0], param);
        var fromExpression = replaceParamVisitor.Visit(ExpressionHelper.UnwrapCastExpression(sourceExpression.Body));

        if (map.IsDynamic)
        {
            fromExpression = node.Property.Type switch
            {
                RqlPropertyType.Reference => MakeReferenceInit(fromExpression, node, map),
                RqlPropertyType.Collection => MakeCollectionInit(fromExpression, node, map),
                _ => fromExpression
            };
        }

        if (!targetType.IsAssignableFrom(fromExpression.Type))
            throw new NotSupportedException($"Cannot map property '{node.Property.Property.Name}' of type {node.Property.Property.DeclaringType!.Name}. Type mismatch.");

        if (targetType != fromExpression.Type)
            fromExpression = Expression.Convert(fromExpression, targetType);

        return fromExpression;
    }

    private Expression MakeReferenceInit(Expression fromExpression, IRqlNode node, RqlMapEntry map)
    {
        var innerMap = GetInnerMapFromEntry(fromExpression.Type, map);

        Expression navExpression = fromExpression;
        if (_collector != null && ExpressionHelper.IsExpensiveExpression(fromExpression))
        {
            navExpression = _collector.LiftExpression(fromExpression, node.GetFullPath());
        }

        var subInit = MakeInitExpression(navExpression, node, map.TargetType, innerMap);

        if (node.Property.IsNullable)
        {
            return Expression.Condition(
                Expression.NotEqual(navExpression, Expression.Constant(null, navExpression.Type)),
                subInit,
                Expression.Constant(null, subInit.Type));
        }

        return subInit;
    }

    private Expression MakeCollectionInit(Expression fromExpression, IRqlNode node, RqlMapEntry map)
    {
        // Temporarily only support List
        if (!typeof(IList).IsAssignableFrom(node.Property.Property.PropertyType))
            throw new NotSupportedException($"Cannot map property '{node.Property.Property.Name}' of type {node.Property.Property.DeclaringType!.Name}. Rql temporarily support only list coollections.");

        var srcItemType = fromExpression.Type.GenericTypeArguments[0];

        if (!TypeHelper.IsUserComplexType(srcItemType))
            return fromExpression;

        var innerMap = GetInnerMapFromEntry(srcItemType, map);
        var innerParam = Expression.Parameter(srcItemType);
        var subInit = MakeInitExpression(innerParam, node, map.TargetType, innerMap);
        var selectLambda = Expression.Lambda(subInit, innerParam);
        var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<,>).MakeGenericType(srcItemType, map.TargetType))!;
        var selectCall = Expression.Call(null, functions.GetSelect(), fromExpression, selectLambda);
        return Expression.Call(null, functions.GetToList(), selectCall);
    }

    private IReadOnlyDictionary<string, RqlMapEntry> GetInnerMapFromEntry(Type typeFrom, RqlMapEntry map)
        => map.InlineMap ?? _mapCache.Get(typeFrom, map.TargetType);
}
