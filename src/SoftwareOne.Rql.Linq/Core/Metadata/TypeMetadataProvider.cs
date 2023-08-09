using System.Collections.Concurrent;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core.Metadata;


internal class TypeMetadataProvider : ITypeMetadataProvider
{
    private readonly IPropertyNameProvider _propertyNameProvider;
    private readonly IPropertyMetadataProvider _propertyMetadataProvider;
    private readonly ConcurrentDictionary<Type, (Dictionary<string, RqlPropertyInfo>, Dictionary<string, string>)> _cache;

    public TypeMetadataProvider(IPropertyNameProvider propertyNameProvider, IPropertyMetadataProvider propertyMetadataProvider)
    {
        _cache = new ConcurrentDictionary<Type, (Dictionary<string, RqlPropertyInfo>, Dictionary<string, string>)>();
        _propertyNameProvider = propertyNameProvider;
        _propertyMetadataProvider = propertyMetadataProvider;
    }

    public string GetDisplayName(Type type, string name)
    {
        return GetCache(type).DisplayNameByName.TryGetValue(name, out string? displayName) ? displayName : name;
    }

    public Dictionary<string, RqlPropertyInfo> ListProperties(Type type)
    {
        return GetCache(type).PropertyByDisplayName;
    }

    public RqlPropertyInfo? GetPropertyByDisplayName(Type type, string displayName)
    {
        if (GetCache(type).PropertyByDisplayName.TryGetValue(displayName, out RqlPropertyInfo? property))
            return property;
        return null;
    }

    private (Dictionary<string, RqlPropertyInfo> PropertyByDisplayName, Dictionary<string, string> DisplayNameByName) GetCache(Type type)
    {
        return _cache.GetOrAdd(type, t =>
        {
            var props = new Dictionary<string, RqlPropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            var disp = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var properties = t.GetProperties().Where(a => a.MemberType.Equals(MemberTypes.Property));

            var member = t.GetCustomAttributes<RqlMemberAttribute>(true).FirstOrDefault();

            foreach (var property in properties)
            {
                var name = _propertyNameProvider.GetName(property);
                props.Add(name, _propertyMetadataProvider.MakeRqlPropertyInfo(property, member));
                disp.Add(property.Name, name);
            }
            return (props, disp);
        });
    }
}