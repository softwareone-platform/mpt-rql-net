using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Context;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

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

    private MemberInitExpression MakeInitExpression(Expression param, RqlNode rqlNode, Type typeFrom, Type typeTo)
    {
        var typeMap = _mapCache.Get(typeFrom, typeTo);

        var bindings = new List<MemberBinding>(_queryContext.Graph.Count);

        foreach (var node in rqlNode.Children.Where(t => t.IsIncluded))
        {
            if (typeMap.TryGetValue(node.Property.Property.Name, out var map))
            {
                var fromExpression = MakeBindExpression(param, node, map.Expression, map.IsDynamic);
                bindings.Add(Expression.Bind(node.Property.Property, fromExpression));
            }
        }

        return Expression.MemberInit(Expression.New(typeTo.GetConstructor(Type.EmptyTypes)!), bindings);
    }

    private Expression MakeBindExpression(Expression param, RqlNode node, LambdaExpression sourceExpression, bool isDynamic)
    {
        var targetType = node.Property.Property.PropertyType;
        var replaceParamVisitor = new ReplaceParameterVisitor(sourceExpression.Parameters[0], param);
        var fromExpression = replaceParamVisitor.Visit(sourceExpression.Body);

        if (isDynamic)
        {
            fromExpression = node.Property.Type switch
            {
                RqlPropertyType.Reference => MakeReferenceInit(fromExpression, node, targetType),
                RqlPropertyType.Collection => MakeCollectionInit(fromExpression, node, node.Property.ElementType!),
                _ => fromExpression
            };
        }

        if (!targetType.IsAssignableFrom(fromExpression.Type))
            throw new NotSupportedException($"Cannot map property '{node.Property.Property.Name}' of type {node.Property.Property.DeclaringType!.Name}. Type mismatch.");

        if (targetType != fromExpression.Type)
            fromExpression = Expression.Convert(fromExpression, targetType);

        return fromExpression;
    }

    private Expression MakeReferenceInit(Expression fromExpression, RqlNode node, Type targetType)
    {
        var subInit = MakeInitExpression(fromExpression, node, fromExpression.Type, targetType);

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

    private Expression MakeCollectionInit(Expression fromExpression, RqlNode node, Type targetItemType)
    {
        var srcItemType = fromExpression.Type.GenericTypeArguments[0];

        if (!TypeHelper.IsUserComplexType(srcItemType))
            return fromExpression;

        var innerParam = Expression.Parameter(srcItemType);
        var subInit = MakeInitExpression(innerParam, node, srcItemType, targetItemType);
        var selectLambda = Expression.Lambda(subInit, innerParam);
        var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<,>).MakeGenericType(srcItemType, targetItemType))!;
        var selectCall = Expression.Call(null, functions.GetSelect(), fromExpression, selectLambda);
        return Expression.Call(null, functions.GetToList(), selectCall);
    }
}
