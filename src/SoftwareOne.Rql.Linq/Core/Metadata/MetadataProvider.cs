using System.Collections.Concurrent;
using System.Reflection;
using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Linq.Core.Metadata;

internal class MetadataProvider : IMetadataProvider, IRqlMetadataProvider
{
    private readonly IPropertyNameProvider _propertyNameProvider;
    private readonly IMetadataFactory _metadataProvider;
    private readonly ConcurrentDictionary<Type, Dictionary<string, RqlPropertyInfo>> _cache;

    public MetadataProvider(IPropertyNameProvider propertyNameProvider, IMetadataFactory metadataProvider)
    {
        _cache = new ConcurrentDictionary<Type, Dictionary<string, RqlPropertyInfo>>();
        _propertyNameProvider = propertyNameProvider;
        _metadataProvider = metadataProvider;
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
                props.Add(name, _metadataProvider.MakeRqlPropertyInfo(name, property));
            }
            return props;
        });
    }
}
