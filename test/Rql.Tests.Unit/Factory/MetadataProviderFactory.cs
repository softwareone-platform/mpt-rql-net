using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace Rql.Tests.Unit.Factory;

internal static class MetadataProviderFactory
{
    internal static IMetadataProvider Internal()
    {
        return new MetadataProvider(
            new PropertyNameProvider(), 
            new MetadataFactory(RqlSettingsFactory.Default()));
    }

    internal static IRqlMetadataProvider Public()
    {
        return new MetadataProvider(
            new PropertyNameProvider(), 
            new MetadataFactory(RqlSettingsFactory.Default()));
    }
}

