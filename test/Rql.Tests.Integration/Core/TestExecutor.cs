using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq;
using Xunit;

namespace Rql.Tests.Integration.Core;

public abstract class TestExecutor<T> where T : ITestEntity
{
    private readonly IRqlQueryable<T> _rql;

    public TestExecutor()
    {
        _rql = RqlFactory.Make<T>(ConfigureRql);
    }

    public void ResultMatch(Func<IQueryable<T>, IQueryable<T>> configure, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
        => ResultMatch(configure(GetQuery()), filter, order, select, isHappyFlow: isHappyFlow);

    public void ResultMatch(Func<T, bool> predicate, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
        => ResultMatch(GetQuery().Where(predicate).AsEnumerable(), filter, order, select, isHappyFlow: isHappyFlow);

    public void ResultMatch(IEnumerable<T> toCompare, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
    {
        var transformed = _rql.Transform(GetQuery(), new RqlRequest { Filter = filter, Order = order, Select = select });

        Assert.False(transformed.IsError);

        var data = transformed.Value.OfType<ITestEntity>().ToList();

        Assert.True(isHappyFlow == false || data.Any());
        Assert.Equal(isHappyFlow, data.SequenceEqual(toCompare.OfType<ITestEntity>(), new TestEntityEqualityComparer()));
    }

    protected abstract IQueryable<T> GetQuery();

    protected abstract void ConfigureRql(RqlOptions options);
}