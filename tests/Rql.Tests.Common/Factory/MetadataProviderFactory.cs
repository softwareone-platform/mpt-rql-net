using Mpt.Rql.Abstractions;
using Mpt.Rql.Core.Metadata;

namespace Rql.Tests.Common.Factory;

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

