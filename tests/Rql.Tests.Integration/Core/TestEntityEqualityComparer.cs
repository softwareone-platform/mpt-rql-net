using System.Diagnostics.CodeAnalysis;

namespace Rql.Tests.Integration.Core;

internal class TestEntityEqualityComparer : IEqualityComparer<ITestEntity>
{
    public bool Equals(ITestEntity? left, ITestEntity? right)
    {
        return left != null && right != null && left.Id == right.Id;
    }

    public int GetHashCode([DisallowNull] ITestEntity subject)
    {
        return subject.Id.GetHashCode();
    }
}
