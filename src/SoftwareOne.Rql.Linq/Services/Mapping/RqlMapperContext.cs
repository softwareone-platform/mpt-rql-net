using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal class RqlMapperContext<TStorage, TView> : IRqlMapperContext<TStorage, TView>, IRqlMapperContext
{
    private readonly IRqlMetadataProvider _rqlMetadataProvider;
    private readonly Dictionary<string, (LambdaExpression Expression, bool IsDynamic)> _mapping;
    private readonly HashSet<string> _ignored;

    public RqlMapperContext(IRqlMetadataProvider rqlMetadataProvider)
    {
        _mapping = [];
        _ignored = [];
        _rqlMetadataProvider = rqlMetadataProvider;
    }
    public IRqlMapperContext<TStorage, TView> Map<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from) where TTo : TFrom
    {
        _mapping.Add(GetMemberName(to), (from, false));
        return this;
    }

    public IRqlMapperContext<TStorage, TView> MapDynamic<TFrom, TTo>(Expression<Func<TView, TTo?>> to, Expression<Func<TStorage, TFrom?>> from)
    {
        _mapping.Add(GetMemberName(to), (from, true));
        return this;
    }

    public IRqlMapperContext<TStorage, TView> Ignore<TTo>(Expression<Func<TView, TTo?>> toIgnore)
    {
        _ignored.Add(GetMemberName(toIgnore));
        return this;
    }

    Dictionary<string, (LambdaExpression Expression, bool IsDynamic)> IRqlMapperContext.Mapping => _mapping;

    void IRqlMapperContext.AddMissing()
    {
        var toProps = _rqlMetadataProvider.GetPropertiesByDeclaringType(typeof(TView));
        var fromProps = _rqlMetadataProvider.GetPropertiesByDeclaringType(typeof(TStorage)).ToDictionary(k => k.Property.Name);

        foreach (var targetProp in toProps)
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
                _mapping.Add(targetName, (Expression.Lambda(Expression.MakeMemberAccess(param, srcProp.Property), param), true));
            }
        }
    }

    private static string GetMemberName<TTo>(Expression<Func<TView, TTo?>> to)
    {
        var memberExpression = to.Body as MemberExpression ?? throw new RqlMappingException("Path must be a member expression");

        if (memberExpression.Expression is not ParameterExpression)
            throw new RqlMappingException($"Only immediate properties are supported: {memberExpression}");

        return memberExpression.Member.Name;
    }
}