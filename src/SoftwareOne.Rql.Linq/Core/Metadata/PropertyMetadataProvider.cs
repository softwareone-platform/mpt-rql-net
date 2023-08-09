using SoftwareOne.Rql.Linq.Configuration;
using System.Collections;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core.Metadata
{
    internal class PropertyMetadataProvider : IPropertyMetadataProvider
    {
        private readonly IRqlSettings _settings;
        public PropertyMetadataProvider(IRqlSettings settings)
        {
            _settings = settings;
        }

        public RqlPropertyInfo MakeRqlPropertyInfo(string name, PropertyInfo property, RqlMemberAttribute? typeAttribute)
        {
            var pi = new RqlPropertyInfo
            {
                Name = name,
                Property = property,
                Type = GetRqlPropertyType(property),
                Flags = _settings.DefaultMemberFlags
            };

            TryApplyAttributeData(pi, typeAttribute);
            TryApplyAttributeData(pi, property.GetCustomAttributes<RqlMemberAttribute>(true).FirstOrDefault());

            return pi;

            static void TryApplyAttributeData(RqlPropertyInfo property, RqlMemberAttribute? attribute)
            {
                if (attribute != null)
                {
                    property.Flags = attribute.Flags;
                }
            }
        }

        private static RqlPropertyType GetRqlPropertyType(PropertyInfo property)
        {
            if (IsUserComplexType(property.PropertyType))
            {
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    return RqlPropertyType.Collection;
                return RqlPropertyType.Reference;
            }
            else
            {
                if (property.PropertyType == typeof(byte[]))
                    return RqlPropertyType.Binary;
                return RqlPropertyType.Primitive;
            }
        }

        private static bool IsUserComplexType(Type type)
        {
            return
                type.IsClass
                && !type.FullName!.StartsWith("System.")
                || typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType && IsUserComplexType(type.GetGenericArguments()[0]);
        }
    }
}
