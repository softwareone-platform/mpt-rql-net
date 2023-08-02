using Rql.Sample.Contracts.InMemory;
using Rql.Tests.Integration.Fixtures;
using Rql.Tests.Integration.Mock;
using Xunit;

namespace Rql.Tests.Integration.Service;

public class TestExecutor
{
    private readonly HttpClient _client;

    public TestExecutor(SampleApiInstanceFixture fixture)
    {
        _client = fixture.Client;
        _client = fixture.Client;
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
}