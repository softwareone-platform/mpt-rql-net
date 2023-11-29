using SoftwareOne.Rql.Linq.Services.Filtering;

namespace Rql.Tests.Unit.Factory;

internal static class PathBuilderFactory
{
    internal static IFilteringPathInfoBuilder Internal()
    {
        return new FilteringPathInfoBuilder(new SimpleActionValidator(), MetadataProviderFactory.Internal());
    }
}

