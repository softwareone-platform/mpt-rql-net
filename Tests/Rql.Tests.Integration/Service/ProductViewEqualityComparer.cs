using Rql.Sample.Contracts.InMemory;
using System.Diagnostics.CodeAnalysis;

namespace Rql.Tests.Integration.Service;

internal class ProductViewEqualityComparer : IEqualityComparer<SampleEntityView>
{
    public bool Equals(SampleEntityView? left, SampleEntityView? right)
    {
        return left != null && right != null && left.Id == right.Id;
    }

    public int GetHashCode([DisallowNull] SampleEntityView subject)
    {
        return subject.Id.GetHashCode();
    }
}
