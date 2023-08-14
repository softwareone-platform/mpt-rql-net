using SoftwareOne.Rql.Linq.Core.Metadata;

namespace Rql.Tests.Unit.Factory;

internal static class TypeMetadataProviderFactory
{
    internal static TypeMetadataProvider Default()
    {
        return new TypeMetadataProvider(
            new PropertyNameProvider(), 
            new PropertyMetadataProvider(RqlSettingsFactory.Default()));
    }
}

