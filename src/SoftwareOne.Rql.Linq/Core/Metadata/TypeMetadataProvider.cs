using System.Collections.Concurrent;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core.Metadata
{

    internal class TypeMetadataProvider : ITypeMetadataProvider
    {
        private readonly IPropertyNameProvider _propertyNameProvider;
        private readonly IPropertyMetadataProvider _propertyMetadataProvider;
        private readonly ConcurrentDictionary<Type, Dictionary<string, RqlPropertyInfo>> _cache;

        public TypeMetadataProvider(IPropertyNameProvider propertyNameProvider, IPropertyMetadataProvider propertyMetadataProvider)
        {
            _cache = new ConcurrentDictionary<Type, Dictionary<string, RqlPropertyInfo>>();
            _propertyNameProvider = propertyNameProvider;
            _propertyMetadataProvider = propertyMetadataProvider;
        }

        public IEnumerable<RqlPropertyInfo> GetProperties(Type type)
            => GetCache(type).Values;

        public bool TryGetPropertyByDisplayName(Type type, string propertyName, out RqlPropertyInfo? rqlProperty)
            => GetCache(type).TryGetValue(propertyName, out rqlProperty);

        public RqlPropertyInfo? GetPropertyByDisplayName(Type type, string displayName)
        {
            if (GetCache(type).TryGetValue(displayName, out RqlPropertyInfo? property))
                return property;
            return null;
        }

        private Dictionary<string, RqlPropertyInfo> GetCache(Type type)
        {
            return _cache.GetOrAdd(type, t =>
            {
                var props = new Dictionary<string, RqlPropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
                var properties = t.GetProperties().Where(a => a.MemberType.Equals(MemberTypes.Property));

                var member = t.GetCustomAttributes<RqlMemberAttribute>(true).FirstOrDefault();

                foreach (var property in properties)
                {
                    var name = _propertyNameProvider.GetName(property);
                    props.Add(name, _propertyMetadataProvider.MakeRqlPropertyInfo(name, property, member));
                }
                return props;
            });
        }
    }
}
