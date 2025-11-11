using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;
public class RqlMapAssessorTests
{
    private IServiceProvider _serviceProvider;

    public RqlMapAssessorTests()
    {
        var services = new ServiceCollection();
        services.AddRql(t =>
        {
            t.ScanForMappers(typeof(RqlMapAssessorTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void GetEntries_WhenCalledForExistingMap_ReturnsMap()
    {
        // Arrange, Act
        var accessor = _serviceProvider.GetRequiredService<IRqlMapAccessor>();
        var entries = accessor.GetEntries<DbEntity, Entity>().ToList();

        // Assert
        entries.Should().HaveCount(3);

        var cache = entries.ToDictionary(t => t.TargetProperty.Property.Name);

        cache["Id"].IsDynamic.Should().BeTrue();
        cache["DsplayName"].IsDynamic.Should().BeFalse();
        cache["Item"].IsDynamic.Should().BeTrue();
    }

    internal class Item
    {
        public string Id { get; set; } = null!;
    }

    internal class DbItem
    {
        public string Id { get; set; } = null!;
    }

    internal class Entity
    {
        public string Id { get; set; } = null!;

        public string DsplayName { get; set; } = null!;

        public Item Item { get; set; } = null!;
    }

    internal class DbEntity
    {
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;

        public DbItem DbItem { get; set; } = null!;
    }

    internal class Mapper : IRqlMapper<DbEntity, Entity>
    {
        public void MapEntity(IRqlMapperContext<DbEntity, Entity> context)
        {
            context.MapStatic(t => t.DsplayName, t => t.Name);
            context.MapDynamic(t => t.Item, t => t.DbItem);
        }
    }
}
