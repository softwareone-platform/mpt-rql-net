using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;

namespace Rql.Tests.Common.Factory;

internal static class PathBuilderFactory
{
    internal static IFilteringPathInfoBuilder Internal()
    {
        return new FilteringPathInfoBuilder(new SimpleActionValidator(), MetadataProviderFactory.Internal(), new BuilderContext(), RqlSettingsFactory.Default());
    }
}

