using Mpt.Rql.Core;

namespace Mpt.Rql.Core.Metadata;

internal interface IMetadataProvider
{
    bool TryGetPropertyByDisplayName(Type type, string propertyName, out RqlPropertyInfo? rqlProperty);

    IEnumerable<RqlPropertyInfo> GetPropertiesByDeclaringType(Type type);
}
