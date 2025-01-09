using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Services.Mapping;
public class CollectionMappingTests
{
    private IServiceProvider _serviceProvider;

    public CollectionMappingTests()
    {
        var services = new ServiceCollection();
        services.AddRql(t =>
        {
            t.ScanForMappers(typeof(RqlMapAssessorTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void MapCollection_WhenNonListMapped_ThrowsException()
    {
        // Arrange, Act, Assert
        var rql = _serviceProvider.GetRequiredService<IRqlQueryable<CollectionMappingTestDbEntity, CollectionMappingTestIncorrectEntity>>();
        var data = new List<CollectionMappingTestDbEntity>
        {
            new() { Items = [new() { Id = "1" }, new() { Id = "2" }] },
            new() { Items = [new() { Id = "3" }, new() { Id = "4" }] },
        };
        var exception = Assert.Throws<NotSupportedException>(() => rql.Transform(data.AsQueryable(), new RqlRequest()));
        exception.Message.Should().Contain("Rql temporarily support only list coollections");
    }

    [Fact]
    public void MapCollection_WhenListMapped_MappedCorrectly()
    {
        // Arrange, Act, Assert
        var rql = _serviceProvider.GetRequiredService<IRqlQueryable<CollectionMappingTestDbEntity, CollectionMappingTestCorrectEntity>>();
        var data = new List<CollectionMappingTestDbEntity>
        {
            new() { Items = [new() { Id = "1" }, new() { Id = "2" }] },
            new() { Items = [new() { Id = "3" }, new() { Id = "4" }] },
        };

        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();
        result.Should().HaveCount(2);
    }
}

internal class CollectionMappingTestItem
{
    public string Id { get; set; } = null!;
}

internal class CollectionMappingTestIncorrectEntity
{
    [RqlProperty(IsCore = true)]
    public ICollection<CollectionMappingTestItem> Items { get; set; } = null!;
}

internal class CollectionMappingTestCorrectEntity
{
    [RqlProperty(IsCore = true)]
    public List<CollectionMappingTestItem> Items { get; set; } = null!;
}

internal class CollectionMappingTestDbEntity
{
    public ICollection<CollectionMappingTestItem> Items { get; set; } = null!;
}

internal class CollectionMappingTestsMapper : IRqlMapper<MapAssessorTestDbEntity, MapAssessorTestEntity>
{
    public void MapEntity(IRqlMapperContext<MapAssessorTestDbEntity, MapAssessorTestEntity> context)
    {
        context.Map(t => t.DsplayName, t => t.Name);
    }
}
