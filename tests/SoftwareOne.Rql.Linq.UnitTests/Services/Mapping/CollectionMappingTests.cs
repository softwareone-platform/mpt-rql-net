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
        var rql = _serviceProvider.GetRequiredService<IRqlQueryable<DbEntity, IncorrectEntity>>();
        var data = new List<DbEntity>
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
        var rql = _serviceProvider.GetRequiredService<IRqlQueryable<DbEntity, CorrectEntity>>();
        var data = new List<DbEntity>
        {
            new() { Items = [new() { Id = "1" }, new() { Id = "2" }] },
            new() { Items = [new() { Id = "3" }, new() { Id = "4" }] },
        };

        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();
        result.Should().HaveCount(2);
    }

    internal class Item
    {
        public string Id { get; set; } = null!;
    }

    internal class IncorrectEntity
    {
        [RqlProperty(IsCore = true)]
        public ICollection<Item> Items { get; set; } = null!;
    }

    internal class CorrectEntity
    {
        [RqlProperty(IsCore = true)]
        public List<Item> Items { get; set; } = null!;
    }

    internal class DbEntity
    {
        public ICollection<Item> Items { get; set; } = null!;
    }
}

