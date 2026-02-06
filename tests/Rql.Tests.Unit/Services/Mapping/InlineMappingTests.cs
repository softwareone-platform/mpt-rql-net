using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;

public class InlineMappingTests
{
    [Fact]
    public void MapCollection_WhenListMapped_MappedCorrectly()
    {
        // Arrange, Act, Assert
        var sp = MakeServiceProvider(t =>
        {
            t.AddScoped(typeof(IRqlMapper<DbEntity, Entity>), typeof(InlineMapper));
        });
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Items = [new() { Id = "1", DbName = "One" }, new() { Id = "2", DbName = "Two" }] },
            new() { Items = [new() { Id = "3", DbName = "Three" }, new() { Id = "4" , DbName = "Four" }] },
        };

        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();
        result.Should().HaveCount(2);
        result[0].Items.Should().HaveCount(2);

        result[0].Items[0].Id.Should().Be("1");
        result[0].Items[0].Name.Should().Be("One");

        result[0].Items[1].Id.Should().Be("2");
        result[0].Items[1].Name.Should().Be("Two");

        result[1].Items[0].Id.Should().Be("3");
        result[1].Items[0].Name.Should().Be("Three");

        result[1].Items[1].Id.Should().Be("4");
        result[1].Items[1].Name.Should().Be("Four");
    }

    [Fact]
    public void MapCollection_WhenListNotMapped_NotMapped()
    {
        // Arrange, Act, Assert
        var sp = MakeServiceProvider(t => { });
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Items = [new() { Id = "1", DbName = "One" }, new() { Id = "2", DbName = "Two" }] },
            new() { Items = [new() { Id = "3", DbName = "Three" }, new() { Id = "4" , DbName = "Four" }] },
        };

        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();
        result.Should().HaveCount(2);
        result[0].Items.Should().HaveCount(2);
        result[0].Items[0].Id.Should().Be("1");
        result[0].Items[0].Name.Should().BeNull();

        result[0].Items[1].Id.Should().Be("2");
        result[0].Items[1].Name.Should().BeNull();

        result[1].Items[0].Id.Should().Be("3");
        result[1].Items[0].Name.Should().BeNull();

        result[1].Items[1].Id.Should().Be("4");
        result[1].Items[1].Name.Should().BeNull();
    }

    private static ServiceProvider MakeServiceProvider(Action<ServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddRql(t =>
        {
            t.Settings.Select.Implicit = t.Settings.Select.Explicit = RqlSelectModes.All;
        });
        configure(services);
        return services.BuildServiceProvider();
    }

    internal class Item
    {
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;
    }

    internal class DbItem
    {
        public string Id { get; set; } = null!;

        public string DbName { get; set; } = null!;
    }

    internal class Entity
    {
        public Item Item { get; set; } = null!;

        [RqlProperty(IsCore = true)]
        public List<Item> Items { get; set; } = null!;
    }

    internal class DbEntity
    {
        public DbItem DbItem { get; set; } = null!;

        public ICollection<DbItem> Items { get; set; } = null!;
    }

    internal class InlineMapper : IRqlMapper<DbEntity, Entity>
    {
        public void MapEntity(IRqlMapperContext<DbEntity, Entity> context)
        {
            context.MapDynamic(t => t.Items, t => t.Items, m =>
            {
                m.MapStatic(t => t.Name, t => t.DbName);
            });
        }
    }
}
