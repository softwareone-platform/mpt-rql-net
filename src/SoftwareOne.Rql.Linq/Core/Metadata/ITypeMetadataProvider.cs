namespace SoftwareOne.Rql.Linq.Core.Metadata;

internal interface ITypeMetadataProvider
{
    string GetDisplayName(Type type, string propertyName);

    RqlPropertyInfo? GetPropertyByDisplayName(Type type, string displayName);

    Dictionary<string, RqlPropertyInfo> ListProperties(Type type);
}