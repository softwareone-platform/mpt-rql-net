using Rql.Sample.Contracts.InMemory;
using System.Diagnostics.CodeAnalysis;

namespace Rql.Tests.Integration.Core
{
    internal class ProductVewEqualityComparer : IEqualityComparer<SampleEntityView>
    {
        public bool Equals(SampleEntityView? x, SampleEntityView? y)
        {
            return x != null && y != null && x.Id == y.Id;
        }

        public int GetHashCode(SampleEntityView obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
