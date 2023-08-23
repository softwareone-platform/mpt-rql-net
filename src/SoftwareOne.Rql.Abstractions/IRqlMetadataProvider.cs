namespace SoftwareOne.Rql.Abstractions;

public interface IRqlMetadataProvider
{
    IEnumerable<IRqlPropertyInfo> GetPropertiesByDeclaringType(Type type);
}