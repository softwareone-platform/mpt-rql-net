using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Exception;
using System.Collections;
using System.Reflection;

namespace Mpt.Rql.Core.Metadata;

internal class MetadataFactory : IMetadataFactory
{
    private readonly IRqlGlobalSettings _settings;
    public MetadataFactory(IRqlGlobalSettings settings)
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
            Actions = _settings.General.DefaultActions,
            Operators = _settings.General.AllowedOperators,
            ElementType = GetCollectionElementType(property.PropertyType),
            IsNullable = IsNullable(property)
        };

        var attribute = property.GetCustomAttributes<RqlPropertyAttribute>(true).FirstOrDefault();

        if (attribute != null)
        {
            propertyInfo.IsCore = attribute.IsCore;
            propertyInfo.Mode = attribute.Mode;

            if (attribute.ActionStrategy != null)
            {
                if (!typeof(IActionStrategy).IsAssignableFrom(attribute.ActionStrategy))
                    throw new RqlInvalidActionStrategyException(
                        $"Type {attribute.ActionStrategy.FullName} defined as action strategy for property " +
                        $"({property.DeclaringType!.FullName}).{property.Name} " +
                        $"does not implement {typeof(IActionStrategy).FullName}");

                propertyInfo.ActionStrategy = attribute.ActionStrategy;
            }

            if (attribute.CustomResolver != null)
            {
                if (!typeof(IRqlCustomPropertyResolver).IsAssignableFrom(attribute.CustomResolver))
                    throw new RqlInvalidCustomResolverException(
                        $"Type {attribute.CustomResolver.FullName} defined as custom resolver for property " +
                        $"({property.DeclaringType!.FullName}).{property.Name} " +
                        $"does not implement {typeof(IRqlCustomPropertyResolver).FullName}");

                propertyInfo.CustomResolver = attribute.CustomResolver;
            }

            if (attribute.ActionsSet)
                propertyInfo.Actions = attribute.Actions;

            if (attribute.OperatorsSet)
                propertyInfo.Operators = attribute.Operators;

            if (attribute.SelectSet)
                propertyInfo.SelectModeOverride = attribute.Select;

            if (attribute.TreatAsSet)
                propertyInfo.TypeOverride = attribute.TreatAs;

            if (attribute.IsNullableSet)
                propertyInfo.IsNullable = attribute.IsNullable;

            if (attribute.DependsOn is { Length: > 0 } dependsOn)
            {
                propertyInfo.DependsOn = ValidateDependencies(property, attribute.Mode, dependsOn);
            }
        }

        propertyInfo.Operators &= propertyInfo.Type switch
        {
            RqlPropertyType.Primitive => GetOperatorsForSimpleProperty(propertyInfo),
            RqlPropertyType.Collection => RqlOperators.CollectionDefaults,
            _ => RqlOperators.None,
        };

        return propertyInfo;
    }

    private static IReadOnlyList<string> ValidateDependencies(PropertyInfo property, RqlPropertyMode mode, string[] dependsOn)
    {
        var owner = $"({property.DeclaringType!.FullName}).{property.Name}";

        // an ignored property is excluded from projection by its contract, so nothing should bring its dependencies in
        if (mode == RqlPropertyMode.Ignored)
        {
            throw new RqlInvalidDependencyException($"Property {owner} is ignored and cannot declare dependencies.");
        }

        // GetProperty(name) throws when a property hidden with new changes its type, as both are then listed
        var siblings = property.ReflectedType!.GetProperties();

        foreach (var name in dependsOn)
        {
            if (name == property.Name)
            {
                throw new RqlInvalidDependencyException($"Property {owner} cannot depend on itself.");
            }

            var dependency = siblings.FirstOrDefault(t => t.Name == name)
                ?? throw new RqlInvalidDependencyException($"Property {owner} depends on '{name}', which is not a property of its type.");

            // auto-mapping never binds an ignored property, so the getter would read nothing
            if (dependency.GetCustomAttributes<RqlPropertyAttribute>(true).FirstOrDefault()?.Mode == RqlPropertyMode.Ignored)
            {
                throw new RqlInvalidDependencyException($"Property {owner} depends on '{name}', which is ignored.");
            }

            // a dependency is projected without the defaults beneath it, which only a primitive does not need
            if (GetRqlPropertyType(dependency) != RqlPropertyType.Primitive)
            {
                throw new RqlInvalidDependencyException($"Property {owner} depends on '{name}', which is not a primitive property.");
            }
        }

        return dependsOn.Distinct().ToArray();
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

        return RqlPropertyType.Primitive;
    }

    private static bool IsNullable(PropertyInfo property)
    {
        // Check for nullable value types like int?, DateTime?
        if (Nullable.GetUnderlyingType(property.PropertyType) != null)
            return true;

        // Check for nullable reference types like string?
        var context = new NullabilityInfoContext();
        var nullabilityInfo = context.Create(property);
        return nullabilityInfo.ReadState == NullabilityState.Nullable &&
               !property.PropertyType.IsValueType;
    }

}