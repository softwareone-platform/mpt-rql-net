namespace SoftwareOne.Rql.Linq.Core.Metadata;

internal interface ITypeMetadataProvider
{
    bool TryGetPropertyByDisplayName(Type type, string propertyName, out RqlPropertyInfo? rqlProperty);

    IEnumerable<RqlPropertyInfo> GetProperties(Type type);
}
