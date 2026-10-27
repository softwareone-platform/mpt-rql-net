using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;

public class CollectionScopeMappingTests
{
    [Fact]
    public void Transform_WhenMultipleScopesMatch_AppliesAllRestrictions()
    {
        using var services = CreateServices();
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = CreateStorage();
        var query = new[] { source }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.IsActive);

            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.TenantId == 7);
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items!.Select(item => item.Id).Should().Equal(2);
        source.Items!.Select(item => item.Id).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Transform_WhenAllItemsAreExcluded_ReturnsEmptyCollection()
    {
        using var services = CreateServices();
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var query = new[] { CreateStorage() }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.TenantId == 99);
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Transform_WhenCollectionIsNullAndNavigationIsSafe_ReturnsNull()
    {
        using var services = CreateServices();
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var source = new Storage { Items = null };
        var query = new[] { source }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.Settings.Mapping.Navigation = NavigationStrategy.Safe;
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.IsActive);
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Should().BeNull();
    }

    [Fact]
    public void Transform_WhenNestedPathMatches_FiltersNestedSource()
    {
        using var services = CreateServices();
        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, NestedView>>();
        var query = new[] { CreateStorage() }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Account!.Items!,
                item => item.IsActive);
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items!.Select(item => item.Id).Should().Equal(2, 3);
    }

    [Theory]
    [MemberData(nameof(MissingAccounts))]
    public void Transform_WhenNestedSourceIsMissingAndNavigationIsSafe_ReturnsNull(
        Account? account)
    {
        using var services = CreateServices();
        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, NestedView>>();
        var source = new Storage { Account = account };
        var query = new[] { source }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.Settings.Mapping.Navigation = NavigationStrategy.Safe;
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Account!.Items!,
                item => item.IsActive);
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Should().BeNull();
    }

    [Fact]
    public void Transform_WhenTargetsHaveSupportedTypes_MaterializesCollections()
    {
        using var services = CreateServices();
        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, CollectionShapesView>>();
        var query = new[] { CreateStorage() }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.IsActive);

            options.ScopeCollection<Storage, string>(
                storage => storage.Tags!,
                tag => tag != "a");
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Select(item => item.Id).Should().Equal(2, 3);
        result.StaticItems.Select(item => item.Id).Should().Equal(2, 3);
        result.Tags.Should().Equal("b", "c");
    }

    [Fact]
    public void Transform_WhenStaticSourceIsNullAndNavigationIsSafe_ReturnsNull()
    {
        using var services = CreateServices();
        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, CollectionShapesView>>();
        var source = new Storage { Items = null, Tags = null };
        var query = new[] { source }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.Settings.Mapping.Navigation = NavigationStrategy.Safe;
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.IsActive);

            options.ScopeCollection<Storage, string>(
                storage => storage.Tags!,
                tag => tag != "a");
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.StaticItems.Should().BeNull();
        result.Tags.Should().BeNull();
    }

    [Theory]
    [InlineData(7, 2)]
    [InlineData(99, null)]
    public void Transform_WithTakeFirst_AppliesScopeBeforeSelectingItem(
        int tenantId,
        int? expectedId)
    {
        using var services = CreateServices();
        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, FirstItemView>>();
        var query = new[] { CreateStorage() }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.IsActive && item.TenantId == tenantId);
        });
        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        (result.FirstItem?.Id).Should().Be(expectedId);
    }

    [Fact]
    public void Transform_WhenTargetCollectionIsUnsupported_Throws()
    {
        using var services = CreateServices();
        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, UnsupportedView>>();
        var query = new[] { CreateStorage() }.AsQueryable();

        Action act = () =>
        {
            rql.Transform(query, new RqlRequest(), options =>
            {
                options.ScopeCollection<Storage, StorageItem>(
                    storage => storage.Items!,
                    item => item.IsActive);
            });
        };

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*only list*");
    }

    [Fact]
    public void Transform_WhenMappingIsTransparentAndScopeExists_Throws()
    {
        using var services = CreateServices();
        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, Storage>>();
        var query = new[] { CreateStorage() }.AsQueryable();

        Action act = () =>
        {
            rql.Transform(query, new RqlRequest(), options =>
            {
                options.Settings.Mapping.Transparent = true;
                options.ScopeCollection<Storage, StorageItem>(
                    storage => storage.Items!,
                    item => item.IsActive);
            });
        };

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*transparent mapping*");
    }

    [Fact]
    public void Transform_WhenNextCallHasNoScopes_DoesNotReusePreviousRules()
    {
        using var services = CreateServices();
        var rql = services.GetRequiredService<IRqlQueryable<Storage, View>>();
        var query = new[] { CreateStorage() }.AsQueryable();

        var restrictedResponse = rql.Transform(
            query,
            new RqlRequest(),
            options => options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.IsActive));

        var unrestrictedResponse = rql.Transform(query, new RqlRequest());

        var restricted = restrictedResponse.Query.Single();
        var unrestricted = unrestrictedResponse.Query.Single();

        restrictedResponse.IsSuccess.Should().BeTrue();
        unrestrictedResponse.IsSuccess.Should().BeTrue();
        restricted.Items!.Select(item => item.Id).Should().Equal(2, 3);
        unrestricted.Items!.Select(item => item.Id).Should().Equal(1, 2, 3);
    }
    
    [Theory]
    [InlineData(NavigationStrategy.Default)]
    [InlineData(NavigationStrategy.Safe)]
    public void Transform_WithoutScopes_PreservesPrimitiveCollectionInstance(
        NavigationStrategy navigation)
    {
        using var services = CreateServices();

        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, CollectionShapesView>>();

        var source = CreateStorage();
        var query = new[] { source }.AsQueryable();

        var response = rql.Transform(
            query,
            new RqlRequest(),
            options => options.Settings.Mapping.Navigation = navigation);

        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Tags.Should().BeSameAs(source.Tags);
        result.Tags.Should().Equal("a", "b", "c");
    }
    
    [Fact]
    public void Transform_WithScopeOnDifferentCollection_PreservesPrimitiveCollectionInstance()
    {
        using var services = CreateServices();

        var rql = services
            .GetRequiredService<IRqlQueryable<Storage, CollectionShapesView>>();

        var source = CreateStorage();
        var query = new[] { source }.AsQueryable();

        var response = rql.Transform(query, new RqlRequest(), options =>
        {
            options.ScopeCollection<Storage, StorageItem>(
                storage => storage.Items!,
                item => item.IsActive);
        });

        var result = response.Query.Single();

        response.IsSuccess.Should().BeTrue();
        result.Items.Select(item => item.Id).Should().Equal(2, 3);
        result.Tags.Should().BeSameAs(source.Tags);
    }

    public static IEnumerable<object?[]> MissingAccounts()
    {
        yield return new object?[] { null };
        yield return new object?[] { new Account { Items = null } };
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services.AddRql(options =>
        {
            options.Settings.Mapping.Transparent = false;
            options.Settings.Mapping.Navigation = NavigationStrategy.Default;
        });

        services.AddSingleton<
            IRqlMapper<Storage, NestedView>,
            NestedMapper>();

        services.AddSingleton<
            IRqlMapper<Storage, CollectionShapesView>,
            CollectionShapesMapper>();

        services.AddSingleton<
            IRqlMapper<Storage, FirstItemView>,
            FirstItemMapper>();

        services.AddTransient<FirstItemFactory>();

        return services.BuildServiceProvider();
    }

    private static Storage CreateStorage()
    {
        var items = new List<StorageItem>
        {
            new() { Id = 1, IsActive = false, TenantId = 7 },
            new() { Id = 2, IsActive = true, TenantId = 7 },
            new() { Id = 3, IsActive = true, TenantId = 8 }
        };

        return new Storage
        {
            Items = items,
            Account = new Account { Items = items },
            Tags = ["a", "b", "c"]
        };
    }

    public sealed class Storage
    {
        public List<StorageItem>? Items { get; set; } = [];
        public Account? Account { get; set; }
        public List<string>? Tags { get; set; } = [];
    }

    public sealed class Account
    {
        public List<StorageItem>? Items { get; set; } = [];
    }

    public sealed class StorageItem
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        public bool IsActive { get; set; }
        public int TenantId { get; set; }
    }

    public sealed class ItemView
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }
    }

    public sealed class View
    {
        [RqlProperty(IsCore = true)]
        public List<ItemView>? Items { get; set; }
    }

    public sealed class NestedView
    {
        [RqlProperty(IsCore = true)]
        public List<ItemView>? Items { get; set; }
    }

    public sealed class CollectionShapesView
    {
        [RqlProperty(IsCore = true)]
        public List<ItemView> Items { get; set; } = [];

        [RqlProperty(IsCore = true)]
        public List<StorageItem> StaticItems { get; set; } = [];

        [RqlProperty(IsCore = true)]
        public List<string> Tags { get; set; } = [];
    }

    public sealed class FirstItemView
    {
        [RqlProperty(IsCore = true)]
        public ItemView? FirstItem { get; set; }
    }

    public sealed class UnsupportedView
    {
        [RqlProperty(IsCore = true)]
        public HashSet<ItemView> Items { get; set; } = [];
    }

    public sealed class NestedMapper : IRqlMapper<Storage, NestedView>
    {
        public void MapEntity(IRqlMapperContext<Storage, NestedView> context)
        {
            context.MapDynamic<StorageItem, ItemView>(
                view => view.Items,
                storage => storage.Account!.Items);
        }
    }

    public sealed class CollectionShapesMapper
        : IRqlMapper<Storage, CollectionShapesView>
    {
        public void MapEntity(
            IRqlMapperContext<Storage, CollectionShapesView> context)
        {
            context.MapStatic(
                view => view.StaticItems,
                storage => storage.Items);
        }
    }

    public sealed class FirstItemMapper : IRqlMapper<Storage, FirstItemView>
    {
        public void MapEntity(
            IRqlMapperContext<Storage, FirstItemView> context)
        {
            context.MapWithFactory<FirstItemFactory>(
                view => view.FirstItem);
        }
    }

    public sealed class FirstItemFactory : IRqlMappingExpressionFactory<Storage>
    {
        public ExpressionFactoryHint Hint => ExpressionFactoryHint.TakeFirst;

        public Expression<Func<Storage, object?>> GetStorageExpression()
        {
            return storage => storage.Items;
        }
    }
}