using SoftwareOne.Rql.Abstractions.Exception;
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
            Operators = _settings.AllowedOperators,
            ElementType = GetCollectionElementType(property.PropertyType)
        };

        var attribute = property.GetCustomAttributes<RqlPropertyAttribute>(true).FirstOrDefault();

        if (attribute != null)
        {
            propertyInfo.IsCore = attribute.IsCore;
            propertyInfo.IsNullable = attribute.IsNullable;
            propertyInfo.IsIgnored = attribute.IsIgnored;

            if (attribute.ActionStrategy != null)
            {
                if (!typeof(IActionStrategy).IsAssignableFrom(attribute.ActionStrategy))
                    throw new RqlInvalidActionStrategyException(
                        $"Type {attribute.ActionStrategy.FullName} defined as action strategy for property " +
                        $"({property.DeclaringType!.FullName}).{property.Name} " +
                        $"does not implement {typeof(IActionStrategy).FullName}");

                propertyInfo.ActionStrategy = attribute.ActionStrategy;
            }

            if (attribute.ActionsSet)
                propertyInfo.Actions = attribute.Actions;

            if (attribute.OperatorsSet)
                propertyInfo.Operators = attribute.Operators;

            if (attribute.SelectSet)
                propertyInfo.SelectMode = attribute.Select;
        }

        propertyInfo.Operators &= propertyInfo.Type switch
        {
            RqlPropertyType.Primitive => GetOperatorsForSimpleProperty(propertyInfo),
            RqlPropertyType.Collection => RqlOperators.CollectionDefaults,
            _ => RqlOperators.None,
        };

        return propertyInfo;
    }

    private static RqlOperators GetOperatorsForSimpleProperty(RqlPropertyInfo propertyInfo)
    {
        Type propType = propertyInfo.Property!.PropertyType;
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

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType()!;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        var enumType = type.GetInterfaces()
            .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();

        return enumType ?? default;
    }


    private static RqlPropertyType GetRqlPropertyType(PropertyInfo property)
    {
        var type = property.PropertyType;

        if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
            return RqlPropertyType.Collection;

        if (TypeHelper.IsUserComplexType(type))
            return RqlPropertyType.Reference;

        if (type == typeof(byte[]))
            return RqlPropertyType.Binary;

        return RqlPropertyType.Primitive;
    }
}