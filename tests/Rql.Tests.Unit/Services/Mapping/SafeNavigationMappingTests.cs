using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;

public class SafeNavigationMappingTests
{
    [Fact]
    public void Apply_SafeNavigationEnabled_ReferenceProperty_WhenNull_ReturnsNull()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Id = "1", Name = "Test" } // Reference is null
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("1");
        result[0].Reference.Should().BeNull();
    }

    [Fact]
    public void Apply_SafeNavigationEnabled_ReferenceProperty_WhenSet_ReturnsMapped()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Id = "1", Name = "Test", DbReference = new DbReference { RefId = 42, RefName = "RefValue" } }
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Reference.Should().NotBeNull();
        result[0].Reference!.RefId.Should().Be(42);
        result[0].Reference!.RefName.Should().Be("RefValue");
    }

    [Fact]
    public void Apply_SafeNavigationDisabled_ReferenceProperty_WhenNull_ThrowsNullReference()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Default);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Id = "1", Name = "Test" } // Reference is null
        };

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList());
    }

    [Fact]
    public void Apply_SafeNavigationEnabled_CollectionProperty_WhenNull_ReturnsNull()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Id = "1", Name = "Test" } // Items collection is null
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("1");
        result[0].Items.Should().BeNull();
    }

    [Fact]
    public void Apply_SafeNavigationEnabled_CollectionProperty_WhenSet_ReturnsMapped()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Id = "1", Name = "Test", DbItems = [new() { ItemId = "i1" }, new() { ItemId = "i2" }] }
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Items.Should().HaveCount(2);
        result[0].Items[0].ItemId.Should().Be("i1");
        result[0].Items[1].ItemId.Should().Be("i2");
    }

    [Fact]
    public void Apply_SafeNavigationDisabled_CollectionProperty_WhenNull_ThrowsNullReference()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Default);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntity, Entity>>();
        var data = new List<DbEntity>
        {
            new() { Id = "1", Name = "Test" } // Items collection is null
        };

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList());
    }

    [Fact]
    public void Apply_SafeNavigationEnabled_NestedPropertyAccess_WhenIntermediateNull_ReturnsNull()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntityWithNested, EntityWithNested>>();
        var data = new List<DbEntityWithNested>
        {
            new() { Id = "1" } // Nested is null, but we try to access Nested.Value
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("1");
        result[0].NestedValue.Should().BeNull();
    }

    [Fact]
    public void Apply_SafeNavigationEnabled_NestedPropertyAccess_WhenSet_ReturnsMapped()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntityWithNested, EntityWithNested>>();
        var data = new List<DbEntityWithNested>
        {
            new() { Id = "1", Nested = new NestedObject { Value = "NestedValue" } }
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].NestedValue.Should().Be("NestedValue");
    }

    [Fact]
    public void Apply_SafeNavigationDisabled_NestedPropertyAccess_WhenIntermediateNull_ThrowsNullReference()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Default);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntityWithNested, EntityWithNested>>();
        var data = new List<DbEntityWithNested>
        {
            new() { Id = "1" } // Nested is null
        };

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
            rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList());
    }

    [Fact]
    public void Apply_SafeNavigationEnabled_ValueTypeProperty_WhenIntermediateNull_ReturnsNull()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntityWithValueType, EntityWithValueType>>();
        var data = new List<DbEntityWithValueType>
        {
            new() { Id = "1" } // Nested is null but we access Nested.Count
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Count.Should().BeNull();
    }


    [Fact]
    public void Apply_SafeNavigationEnabled_ValueTypeProperty_WhenSet_ReturnsMapped()
    {
        // Arrange
        var sp = MakeServiceProvider(NavigationStrategy.Safe);
        var rql = sp.GetRequiredService<IRqlQueryable<DbEntityWithValueType, EntityWithValueType>>();
        var data = new List<DbEntityWithValueType>
        {
            new() { Id = "1", Nested = new NestedWithValueType { Count = 42 } }
        };

        // Act
        var result = rql.Transform(data.AsQueryable(), new RqlRequest()).Query.ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Count.Should().Be(42);
    }

    private static ServiceProvider MakeServiceProvider(NavigationStrategy navigation)
    {
        var services = new ServiceCollection();
        services.AddRql(t =>
        {
            t.Settings.Select.Implicit = t.Settings.Select.Explicit = RqlSelectModes.All;
            t.Settings.Mapping.Navigation = navigation;
            t.ScanForMappers(typeof(SafeNavigationMappingTests).Assembly);
        });
        return services.BuildServiceProvider();
    }

    internal class Item
    {
        public string ItemId { get; set; } = null!;
    }

    internal class DbItem
    {
        public string ItemId { get; set; } = null!;
    }

    internal class Reference
    {
        public int RefId { get; set; }
        public string RefName { get; set; } = null!;
    }

    internal class DbReference
    {
        public int RefId { get; set; }
        public string RefName { get; set; } = null!;
    }

    internal class Entity
    {
        [RqlProperty(IsCore = true)]
        public string Id { get; set; } = null!;

        [RqlProperty(IsCore = true)]
        public Reference Reference { get; set; } = null!;

        [RqlProperty(IsCore = true)]
        public List<Item> Items { get; set; } = null!;
    }

    internal class DbEntity
    {
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;

        public DbReference DbReference { get; set; } = null!;

        public ICollection<DbItem> DbItems { get; set; } = null!;
    }

    internal class NestedObject
    {
        public string Value { get; set; } = null!;
    }

    internal class EntityWithNested
    {
        [RqlProperty(IsCore = true)]
        public string Id { get; set; } = null!;

        [RqlProperty(IsCore = true)]
        public string? NestedValue { get; set; }
    }

    internal class DbEntityWithNested
    {
        public string Id { get; set; } = null!;

        public NestedObject Nested { get; set; } = null!;
    }

    internal class NestedWithValueType
    {
        public int? Count { get; set; } = null!;
    }
    
    internal class EntityWithValueType
    {
        [RqlProperty(IsCore = true)]
        public string Id { get; set; } = null!;

        [RqlProperty(IsCore = true)] 
        public int? Count { get; set; } = null!;
    }
    
    internal class DbEntityWithValueType
    {
        public string Id { get; set; } = null!;
    
        public NestedWithValueType Nested { get; set; } = null!;
    }

    internal class Mapper : IRqlMapper<DbEntity, Entity>
    {
        public void MapEntity(IRqlMapperContext<DbEntity, Entity> context)
        {
            context.MapDynamic(t => t.Reference, t => t.DbReference);
            context.MapDynamic(t => t.Items, t => t.DbItems);
        }
    }

    internal class NestedMapper : IRqlMapper<DbEntityWithNested, EntityWithNested>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithNested, EntityWithNested> context)
        {
            context.MapStatic(t => t.NestedValue, t => t.Nested.Value);
        }
    }

    internal class ValueTypeMapper : IRqlMapper<DbEntityWithValueType, EntityWithValueType>
    {
        public void MapEntity(IRqlMapperContext<DbEntityWithValueType, EntityWithValueType> context)
        {
            context.MapStatic(t => t.Count, t => t.Nested.Count);
        }
    }
}
