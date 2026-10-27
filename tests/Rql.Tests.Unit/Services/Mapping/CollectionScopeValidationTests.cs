using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;

public sealed class CollectionScopeValidationTests
{
    [Theory]
    [InlineData(MappingScenario.OrderBy)]
    [InlineData(MappingScenario.Where)]
    [InlineData(MappingScenario.CountProperty)]
    [InlineData(MappingScenario.CountMethod)]
    [InlineData(MappingScenario.Any)]
    [InlineData(MappingScenario.Factory)]
    [InlineData(MappingScenario.Condition)]
    public void Transform_WithScope_RejectsUnsupportedMapping(
        MappingScenario scenario)
    {
        using var services = CreateServices(scenario);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        Action act = () => rql.Transform(
            source,
            new RqlRequest(),
            ApplyItemsScope);

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*unsupported expression*");
    }

    [Theory]
    [InlineData(MappingScenario.OrderBy)]
    [InlineData(MappingScenario.Where)]
    [InlineData(MappingScenario.CountProperty)]
    [InlineData(MappingScenario.CountMethod)]
    [InlineData(MappingScenario.Any)]
    [InlineData(MappingScenario.Factory)]
    [InlineData(MappingScenario.Condition)]
    public void Transform_WithoutScope_DoesNotRejectMapping(
        MappingScenario scenario)
    {
        using var services = CreateServices(scenario);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        var response = rql.Transform(source, new RqlRequest());
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Select(item => item.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Transform_WithValidCollectionAndInvalidCount_StillThrows()
    {
        using var services = CreateServices(MappingScenario.CountProperty);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        Action act = () => rql.Transform(
            source,
            new RqlRequest { Select = "items,count" },
            ApplyItemsScope);

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*Mapping 'Count'*unsupported expression*");
    }

    [Fact]
    public void Transform_WhenUnsupportedPropertyIsNotUsed_DoesNotThrow()
    {
        using var services = CreateServices(MappingScenario.CountProperty);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        var response = rql.Transform(
            source,
            new RqlRequest { Select = "id" },
            options =>
            {
                options.Settings.Select.Implicit = RqlSelectModes.None;
                options.Settings.Select.Explicit = RqlSelectModes.None;
                ApplyItemsScope(options);
            });

        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Id.Should().Be(10);
        result.Count.Should().Be(0);
    }

    [Fact]
    public void Transform_WhenContainingReferenceIsStatic_Throws()
    {
        using var services = CreateServices(MappingScenario.StaticAccount);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        Action act = () => rql.Transform(
            source,
            new RqlRequest(),
            ApplyAccountScope);

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*Mapping 'Account'*unsupported expression*");
    }

    [Fact]
    public void Transform_WhenContainingReferenceIsDynamic_AppliesScope()
    {
        using var services = CreateServices(MappingScenario.Default);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        var response = rql.Transform(
            source,
            new RqlRequest(),
            ApplyAccountScope);

        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Account.Items.Select(item => item.Id).Should().Equal(2);
        result.Items.Select(item => item.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Transform_WhenOnlyOnePathIsScoped_DoesNotRestrictOtherCollection()
    {
        using var services = CreateServices(MappingScenario.Default);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        var response = rql.Transform(
            source,
            new RqlRequest(),
            ApplyItemsScope);

        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Select(item => item.Id).Should().Equal(2);
        result.OtherItems.Select(item => item.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Transform_WhenSelectManyUsesDifferentPath_DoesNotRejectIt()
    {
        using var services = CreateServices(MappingScenario.SelectMany);
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateSource();

        var response = rql.Transform(
            source,
            new RqlRequest(),
            ApplyItemsScope);

        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Select(item => item.Id).Should().Equal(2);
        result.Flattened.Select(item => item.Id).Should().Equal(1, 2);
    }

    private static void ApplyItemsScope(IRqlTransformOptions options)
    {
        options.ScopeCollection<Storage, Item>(
            storage => storage.Items,
            item => item.IsVisible);
    }

    private static void ApplyAccountScope(IRqlTransformOptions options)
    {
        options.ScopeCollection<Storage, Item>(
            storage => storage.Account.Items,
            item => item.IsVisible);
    }

    private static ServiceProvider CreateServices(MappingScenario scenario)
    {
        var services = new ServiceCollection();

        services.AddRql(options =>
        {
            options.Settings.Mapping.Transparent = false;
            options.Settings.Select.Implicit = RqlSelectModes.All;
            options.Settings.Select.Explicit = RqlSelectModes.All;
            options.Settings.Select.MaxDepth = 5;
        });

        services.AddSingleton<IRqlMapper<Storage, View>>(new TestMapper(scenario));
        services.AddTransient<CountFactory>();

        return services.BuildServiceProvider();
    }

    private static IQueryable<Storage> CreateSource()
    {
        var items = new List<Item>
        {
            new() { Id = 1, IsVisible = false },
            new() { Id = 2, IsVisible = true }
        };

        return new[]
        {
            new Storage
            {
                Id = 10,
                Items = items,
                OtherItems = items,
                Account = new Account { Items = items },
                Channels = [new Channel { Items = items }]
            }
        }.AsQueryable();
    }

    public enum MappingScenario
    {
        Default,
        OrderBy,
        Where,
        CountProperty,
        CountMethod,
        Any,
        Factory,
        Condition,
        StaticAccount,
        SelectMany
    }

    public sealed class TestMapper(MappingScenario scenario)
        : IRqlMapper<Storage, View>
    {
        public void MapEntity(IRqlMapperContext<Storage, View> context)
        {
            context.MapDynamic<Item, ItemView>(
                view => view.Items,
                storage => storage.Items);

            switch (scenario)
            {
                case MappingScenario.OrderBy:
                    context.MapDynamic<Item, ItemView>(
                        view => view.OrderedItems,
                        storage => storage.Items.OrderBy(item => item.Id));
                    break;

                case MappingScenario.Where:
                    context.MapDynamic<Item, ItemView>(
                        view => view.OrderedItems,
                        storage => storage.Items.Where(item => item.Id > 0));
                    break;

                case MappingScenario.CountProperty:
                    context.MapStatic(
                        view => view.Count,
                        storage => storage.Items.Count);
                    break;

                case MappingScenario.CountMethod:
                    context.MapStatic(
                        view => view.Count,
                        storage => storage.Items.Count());
                    break;

                case MappingScenario.Any:
                    context.MapStatic(
                        view => view.HasItems,
                        storage => storage.Items.Any());
                    break;

                case MappingScenario.Factory:
                    context.MapWithFactory<CountFactory>(view => view.Count);
                    break;

                case MappingScenario.Condition:
                    context.Switch(view => view.Label)
                        .Case(storage => storage.Items.Any(), storage => "yes")
                        .Default(storage => "no");
                    break;

                case MappingScenario.StaticAccount:
                    context.MapStatic(
                        view => view.Account,
                        storage => storage.Account);
                    break;

                case MappingScenario.SelectMany:
                    context.MapDynamic<Item, ItemView>(
                        view => view.Flattened,
                        storage => storage.Channels.SelectMany(
                            channel => channel.Items));
                    break;
            }
        }
    }

    public sealed class CountFactory : IRqlMappingExpressionFactory<Storage>
    {
        public Expression<Func<Storage, object?>> GetStorageExpression()
            => storage => storage.Items.Count;
    }

    public sealed class Storage
    {
        public int Id { get; set; }
        public List<Item> Items { get; set; } = [];
        public List<Item> OtherItems { get; set; } = [];
        public Account Account { get; set; } = new();
        public List<Channel> Channels { get; set; } = [];
    }

    public sealed class Account
    {
        public List<Item> Items { get; set; } = [];
    }

    public sealed class Channel
    {
        public List<Item> Items { get; set; } = [];
    }

    public sealed class Item
    {
        public int Id { get; set; }
        public bool IsVisible { get; set; }
    }

    public sealed class ItemView
    {
        public int Id { get; set; }
    }

    public sealed class View
    {
        public int Id { get; set; }
        public List<ItemView> Items { get; set; } = [];
        public List<ItemView> OtherItems { get; set; } = [];
        public List<ItemView> OrderedItems { get; set; } = [];
        public List<ItemView> Flattened { get; set; } = [];
        public Account Account { get; set; } = new();
        public int Count { get; set; }
        public bool HasItems { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}