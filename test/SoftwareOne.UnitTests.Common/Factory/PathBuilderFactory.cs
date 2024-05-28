﻿using SoftwareOne.Rql.Linq.Services.Filtering;

namespace SoftwareOne.UnitTests.Common;

internal static class PathBuilderFactory
{
    internal static IFilteringPathInfoBuilder Internal()
    {
        return new FilteringPathInfoBuilder(new SimpleActionValidator(), MetadataProviderFactory.Internal());
    }
}

