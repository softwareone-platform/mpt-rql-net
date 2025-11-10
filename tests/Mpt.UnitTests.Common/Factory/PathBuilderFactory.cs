using Mpt.Rql.Linq.Services.Context;
using Mpt.Rql.Linq.Services.Filtering;

namespace Mpt.UnitTests.Common;

internal static class PathBuilderFactory
{
    internal static IFilteringPathInfoBuilder Internal()
    {
        return new FilteringPathInfoBuilder(new SimpleActionValidator(), MetadataProviderFactory.Internal(), new BuilderContext());
    }
}

