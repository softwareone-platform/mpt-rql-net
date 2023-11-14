using SoftwareOne.Rql;
using System.Linq.Expressions;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Integration.Core;

public abstract class TestExecutor<TStorage> : TestExecutor<TStorage, TStorage> where TStorage : ITestEntity
{
    public void ShapeMatch(Action<TStorage> configure, string select)
    {
        var srcData = GetQuery().ToList();
        var transformed = Rql.Transform(srcData.AsQueryable(), new RqlRequest { Select = select });
        var targetJson = JsonSerializer.Serialize(transformed.Value.ToList());


        foreach (var item in srcData)
        {
            configure(item);
        }
        var srcJson = JsonSerializer.Serialize(srcData);
        Assert.Equal(srcJson, targetJson);
    }

    protected override Expression<Func<TStorage, TStorage>> GetMapping() => t => t;
}

public abstract class TestExecutor<TStorage, TView> where TView : ITestEntity
{
    public TestExecutor()
    {
        Rql = MakeRql();
    }

    public IRqlQueryable<TStorage, TView> Rql { get; init; }

    public abstract IQueryable<TStorage> GetQuery();

    public void ResultMatch(Func<IQueryable<TView>, IQueryable<TView>> configure, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
        => ResultMatch(configure(GetQuery().Select(GetMapping())), filter, order, select, isHappyFlow: isHappyFlow);

    public void ResultMatch(Func<TView, bool> predicate, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
        => ResultMatch(GetQuery().Select(GetMapping()).Where(predicate).AsEnumerable(), filter, order, select, isHappyFlow: isHappyFlow);

    public void ResultMatch(IEnumerable<TView> toCompare, string? filter = null, string? order = null, string? select = null, bool isHappyFlow = true)
    {
        var transformed = Rql.Transform(GetQuery(), new RqlRequest { Filter = filter, Order = order, Select = select });

        Assert.False(transformed.IsError);

        var data = transformed.Value.OfType<ITestEntity>().ToList();

        Assert.True(isHappyFlow == false || data.Any());
        Assert.Equal(isHappyFlow, data.SequenceEqual(toCompare.OfType<ITestEntity>(), new TestEntityEqualityComparer()));
    }

    public void MustFailWithError(string? filter = null, string? order = null, string? select = null, string? errorDescription = null)
    {
        var transformed = Rql.Transform(GetQuery(), new RqlRequest { Filter = filter, Order = order, Select = select });
        Assert.True(transformed.IsError);
        if (errorDescription != null)
            Assert.Equal(errorDescription, transformed.Errors[0].Description);
    }

    protected abstract IRqlQueryable<TStorage, TView> MakeRql();

    protected abstract Expression<Func<TStorage, TView>> GetMapping();
}