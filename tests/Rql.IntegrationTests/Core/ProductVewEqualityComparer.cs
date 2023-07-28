using Rql.Sample.Contracts.InMemory;
using System.Diagnostics.CodeAnalysis;

namespace Rql.IntegrationTests.Core
{
    internal class ProductVewEqualityComparer : IEqualityComparer<SampleEntityView>
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
}
