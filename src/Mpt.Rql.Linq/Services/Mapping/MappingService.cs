using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core.Metadata;
using Mpt.Rql.Linq.Services.Context;
using System.Collections;
using System.Linq.Expressions;

namespace Mpt.Rql.Linq.Services.Mapping;

internal class MappingService<TStorage, TView> : IMappingService<TStorage, TView>
{
    private readonly IQueryContext<TView> _queryContext;
    private readonly IEntityMapCache _mapCache;

    public MappingService(IQueryContext<TView> queryContext, IEntityMapCache mapCache)
    {
        _queryContext = queryContext;
        _mapCache = mapCache;
    }

    public IQueryable<TView> Apply(IQueryable<TStorage> query)
    {
        var param = Expression.Parameter(typeof(TStorage));

        var initExpression = MakeInitExpression(param, _queryContext.Graph, typeof(TStorage), typeof(TView));
        var selector = Expression.Lambda(initExpression!, param);
        return query.Select((Expression<Func<TStorage, TView>>)selector);
    }

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
        var replaceParamVisitor = new ReplaceParameterVisitor(map.SourceExpression.Parameters[0], param);
        var fromExpression = replaceParamVisitor.Visit(map.SourceExpression.Body);

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

        var subInit = MakeInitExpression(fromExpression, node, map.TargetType, innerMap);

        if (node.Property.IsNullable)
        {
            fromExpression = Expression.Condition(Expression.NotEqual(fromExpression, Expression.Constant(null, fromExpression.Type)),
                subInit, Expression.Constant(null, subInit.Type));
        }
        else
        {
            fromExpression = subInit;
        }

        return fromExpression;
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
