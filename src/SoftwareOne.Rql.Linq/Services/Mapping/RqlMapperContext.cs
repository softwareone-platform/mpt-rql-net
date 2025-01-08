using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal abstract class RqlMapperContext
{
    public abstract Dictionary<string, RqlMapEntry> Mapping { get; }

    public abstract void AddMissing();
}

internal class RqlMapperContext<TStorage, TView> : RqlMapperContext, IRqlMapperContext<TStorage, TView>
{
    private readonly IRqlMetadataProvider _rqlMetadataProvider;
    private readonly Dictionary<string, IRqlPropertyInfo> _targetProperties;
    private readonly Dictionary<string, RqlMapEntry> _mapping;
    private readonly HashSet<string> _ignored;

    public RqlMapperContext(IRqlMetadataProvider rqlMetadataProvider)
    {
        _mapping = [];
        _ignored = [];
        _rqlMetadataProvider = rqlMetadataProvider;

        _targetProperties = _rqlMetadataProvider.GetPropertiesByDeclaringType(typeof(TView)).ToDictionary(k => k.Property.Name);
    }

    public IRqlMapperContext<TStorage, TView> Map<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from) where TTo : TFrom
        => MapInternal(to, from, false);

    public IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from)
        => MapInternal(to, from, false);

    public IRqlMapperContext<TStorage, TView> Ignore<TTo>(Expression<Func<TView, TTo?>> toIgnore)
    {
        _ignored.Add(GetTargetProperty(toIgnore).Property.Name);
        return this;
    }

    public override Dictionary<string, RqlMapEntry> Mapping => _mapping;

    public override void AddMissing()
    {
        var fromProps = _rqlMetadataProvider.GetPropertiesByDeclaringType(typeof(TStorage)).ToDictionary(k => k.Property.Name);

        foreach (var targetProp in _targetProperties.Values)
        {
            var targetName = targetProp.Property.Name;

            if (targetProp.IsIgnored)
                continue;

            if (_mapping.ContainsKey(targetName))
                continue;

            if (_ignored.Contains(targetName))
                continue;

            if (fromProps.TryGetValue(targetName, out var srcProp))
            {
                var param = Expression.Parameter(typeof(TStorage));
                var sourceExpression = Expression.Lambda(Expression.MakeMemberAccess(param, srcProp.Property), param);
                MapInternal(targetProp, sourceExpression, true);
            }
        }
    }

    private RqlMapperContext<TStorage, TView> MapInternal<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from, bool isDynamic)
        => MapInternal(GetTargetProperty(to), from, isDynamic);

    private RqlMapperContext<TStorage, TView> MapInternal(IRqlPropertyInfo target, LambdaExpression source, bool isDynamic)
    {
        var mapProperty = new RqlMapEntry
        {
            TargetProperty = target,
            SourceExpression = source,
            IsDynamic = isDynamic
        };

        _mapping.Add(target.Property.Name, mapProperty);
        return this;
    }

    private IRqlPropertyInfo GetTargetProperty<TTo>(Expression<Func<TView, TTo?>> to)
    {
        var memberExpression = to.Body as MemberExpression ?? throw new RqlMappingException("Path must be a member expression");

        if (memberExpression.Expression is not ParameterExpression)
            throw new RqlMappingException($"Only immediate properties are supported: {memberExpression}");

        if (!_targetProperties.TryGetValue(memberExpression.Member.Name, out var target))
            throw new RqlMappingException($"Property {memberExpression.Member.Name} not found in target type");

        return target;
    }
}