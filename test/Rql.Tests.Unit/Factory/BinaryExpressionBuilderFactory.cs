using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Filtering;

namespace Rql.Tests.Unit.Factory;

internal static class BinaryExpressionBuilderFactory
{
    internal static IBinaryExpressionBuilder Internal()
    {
        return new BinaryExpressionBuilder(PathBuilderFactory.Internal());
    }
}

