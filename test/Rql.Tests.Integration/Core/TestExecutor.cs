using SoftwareOne.Rql;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Integration.Core;

public abstract class TestExecutor<TStorage> : TestExecutor<TStorage, TStorage> where TStorage : ITestEntity
{
    protected override Expression<Func<TStorage, TStorage>> GetMapping() => t => t;
}

public abstract class TestExecutor<TStorage, TView> where TView : ITestEntity
{
    private readonly IRqlQueryable<TStorage, TView> _rql;

    public TestExecutor()
    {
        _rql = MakeRql();
    }

    public void ResultMatch(Func<IQueryable<TView>, IQueryable<TView>> configure, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
        => ResultMatch(configure(GetQuery().Select(GetMapping())), filter, order, select, isHappyFlow: isHappyFlow);

    public void ResultMatch(Func<TView, bool> predicate, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
        => ResultMatch(GetQuery().Select(GetMapping()).Where(predicate).AsEnumerable(), filter, order, select, isHappyFlow: isHappyFlow);

    public void ResultMatch(IEnumerable<TView> toCompare, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
    {
        var transformed = _rql.Transform(GetQuery(), new RqlRequest { Filter = filter, Order = order, Select = select });

        Assert.False(transformed.IsError);

        var data = transformed.Value.OfType<ITestEntity>().ToList();

        Assert.True(isHappyFlow == false || data.Any());
        Assert.Equal(isHappyFlow, data.SequenceEqual(toCompare.OfType<ITestEntity>(), new TestEntityEqualityComparer()));
    }

    protected abstract IRqlQueryable<TStorage, TView> MakeRql();

    protected abstract IQueryable<TStorage> GetQuery();

    protected abstract Expression<Func<TStorage, TView>> GetMapping();
}