using Rql.Sample.Contracts.InMemory;
using System.Diagnostics.CodeAnalysis;

namespace Rql.Tests.Integration.Service;

internal class ProductViewEqualityComparer : IEqualityComparer<SampleEntityView>
{
    public bool Equals(SampleEntityView? x, SampleEntityView? y)
    {
        return x != null && y != null && x.Id == y.Id;
    }

    public int GetHashCode([DisallowNull] SampleEntityView obj)
    {
        return obj.Id.GetHashCode();
    }
}
