﻿namespace SoftwareOne.Rql.Linq.Core.Metadata;

internal interface IMetadataProvider
{
    bool TryGetPropertyByDisplayName(Type type, string propertyName, out RqlPropertyInfo? rqlProperty);

    IEnumerable<RqlPropertyInfo> GetPropertiesByDeclaringType(Type type);
}
