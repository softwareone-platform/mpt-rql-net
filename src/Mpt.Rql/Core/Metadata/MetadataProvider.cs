using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using System.Collections.Concurrent;
using System.Reflection;

namespace Mpt.Rql.Core.Metadata;

internal class MetadataProvider : IMetadataProvider, IRqlMetadataProvider
{
    private readonly IPropertyNameProvider _propertyNameProvider;
    private readonly IMetadataFactory _metadataFactory;
    private readonly ConcurrentDictionary<Type, Dictionary<string, RqlPropertyInfo>> _cache;

    public MetadataProvider(IPropertyNameProvider propertyNameProvider, IMetadataFactory metadataFactory)
    {
        _cache = new ConcurrentDictionary<Type, Dictionary<string, RqlPropertyInfo>>();
        _propertyNameProvider = propertyNameProvider;
        _metadataFactory = metadataFactory;
    }

    bool IRqlMetadataProvider.TryGetPropertyByDisplayName(Type type, string propertyName, out IRqlPropertyInfo? rqlProperty)
    {
        var result = ((IMetadataProvider)this).TryGetPropertyByDisplayName(type, propertyName, out var rqlPropertyInfo);
        rqlProperty = rqlPropertyInfo;
        return result;
    }

    IEnumerable<IRqlPropertyInfo> IRqlMetadataProvider.GetPropertiesByDeclaringType(Type type)
        => ((IMetadataProvider)this).GetPropertiesByDeclaringType(type);

    IEnumerable<RqlPropertyInfo> IMetadataProvider.GetPropertiesByDeclaringType(Type type)
        => GetCache(type).Values;

    bool IMetadataProvider.TryGetPropertyByDisplayName(Type type, string propertyName, out RqlPropertyInfo? rqlProperty)
        => GetCache(type).TryGetValue(propertyName, out rqlProperty);

    private Dictionary<string, RqlPropertyInfo> GetCache(Type type)
    {
        return _cache.GetOrAdd(type, t =>
        {
            var props = new Dictionary<string, RqlPropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            var properties = t.GetProperties().Where(a => a.MemberType.Equals(MemberTypes.Property));

            foreach (var property in properties)
            {
                var name = _propertyNameProvider.GetName(property);
                var propertyInfo = _metadataFactory.MakeRqlPropertyInfo(name, property);
                if (!props.TryAdd(name, propertyInfo))
                    props[name] = propertyInfo;
            }
            return props;
        });
    }
}
