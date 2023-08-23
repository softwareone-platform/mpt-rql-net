using SoftwareOne.Rql.Linq.Configuration;
using System.Collections;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core.Metadata;

internal class MetadataFactory : IMetadataFactory
{
    private readonly IRqlSettings _settings;
    public MetadataFactory(IRqlSettings settings)
    {
        _settings = settings;
    }

    public RqlPropertyInfo MakeRqlPropertyInfo(string name, PropertyInfo property)
    {
        var pi = new RqlPropertyInfo
        {
            Name = name,
            Property = property,
            Type = GetRqlPropertyType(property),
            Actions = _settings.DefaultActions
        };

        TryApplyAttributeData(pi, property.GetCustomAttributes<RqlPropertyAttribute>(true).FirstOrDefault());

        return pi;

        static void TryApplyAttributeData(RqlPropertyInfo property, RqlPropertyAttribute? attribute)
        {
            if (attribute != null)
            {
                property.IsDefault = attribute.IsCore;

                if (attribute.ActionsSet)
                    property.Actions = attribute.Actions;
            }
        }
    }

    private static RqlPropertyType GetRqlPropertyType(PropertyInfo property)
    {
        if (IsUserComplexType(property.PropertyType))
        {
            return typeof(IEnumerable).IsAssignableFrom(property.PropertyType) ? RqlPropertyType.Collection : RqlPropertyType.Reference;
        }

        return property.PropertyType == typeof(byte[]) ? RqlPropertyType.Binary : RqlPropertyType.Primitive;
    }

    private static bool IsUserComplexType(Type type)
    {
        return
            type.IsClass
            && !type.FullName!.StartsWith("System.")
            || typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType && IsUserComplexType(type.GetGenericArguments()[0]);
    }
}