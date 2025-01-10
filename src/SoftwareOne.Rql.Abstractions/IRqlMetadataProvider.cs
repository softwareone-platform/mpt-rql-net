namespace SoftwareOne.Rql.Abstractions;

public interface IRqlMetadataProvider
{
    bool TryGetPropertyByDisplayName(Type type, string propertyName, out IRqlPropertyInfo? rqlProperty);

    IEnumerable<IRqlPropertyInfo> GetPropertiesByDeclaringType(Type type);
}