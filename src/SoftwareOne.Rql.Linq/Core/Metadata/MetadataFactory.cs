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
        var propertyInfo = new RqlPropertyInfo
        {
            Name = name,
            Property = property,
            Type = GetRqlPropertyType(property),
            Actions = _settings.DefaultActions,
            Operators = RqlOperators.All
        };

        var attribute = property.GetCustomAttributes<RqlPropertyAttribute>(true).FirstOrDefault();

        if (attribute != null)
        {
            propertyInfo.IsCore = attribute.IsCore;

            if (attribute.ActionsSet)
                propertyInfo.Actions = attribute.Actions;

            if (attribute.OperatorsSet)
                propertyInfo.Operators = attribute.Operators;
        }

        propertyInfo.Operators &= GetOperatorsForProperty(propertyInfo);

        return propertyInfo;
    }

    private static RqlOperators GetOperatorsForProperty(RqlPropertyInfo propertyInfo)
    {
        // Operators are not applicable to complex properties
        if (propertyInfo.Type != RqlPropertyType.Primitive && propertyInfo.Type != RqlPropertyType.Binary)
            return RqlOperators.None;

        Type propType = propertyInfo.Property.PropertyType;
        var innerType = Nullable.GetUnderlyingType(propType);
        propType = innerType ?? propType;

        var operators = RqlOperators.GenericDefaults;

        if (typeof(string).IsAssignableFrom(propType))
            operators = RqlOperators.StringDefaults;
        else if (typeof(Guid).IsAssignableFrom(propType))
            operators = RqlOperators.GuidDefaults;

        if (innerType != null)
            operators |= RqlOperators.Null;

        return operators;
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