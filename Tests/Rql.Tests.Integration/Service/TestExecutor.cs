using Microsoft.AspNetCore.Mvc.Testing;
using Rql.Sample.Contracts.InMemory;
using Rql.Tests.Common;
using Rql.Tests.Integration.Factory;
using Rql.Tests.Integration.Service;
using Xunit;

namespace Rql.Tests.Integration;

public class TestExecutor : IDisposable
{
    private readonly RqlTestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TestExecutor()
    {
        _factory = new RqlTestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task Execute(
        Func<SampleEntityView, bool> filter,
        string? query = null,
        string? order = null,
        string? select = null,
        bool isHappyFlow = true)
    {
        return Execute(MockProductRepository.View.Where(filter), query, order, select, isHappyFlow);
    }

    public async Task Execute(
        IEnumerable<SampleEntityView> toCompare,
        string? query = null,
        string? order = null,
        string? select = null,
        bool isHappyFlow = true)
    {
        var response = await _client.GetAsync($"/memory/sample?query={query}&order={order}&select={select}");
        response.EnsureSuccessStatusCode();
        var respData = (await response.Content.ReadFromJsonAsync<List<SampleEntityView>>())!;

        Assert.True(isHappyFlow == false || respData.Any());
        Assert.Equal(isHappyFlow, respData.SequenceEqual(toCompare, new ProductViewEqualityComparer()));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}