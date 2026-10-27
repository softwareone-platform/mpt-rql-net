using System.Linq.Expressions;
using FluentAssertions;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Settings;
using Xunit;

namespace Rql.Tests.Unit.Services.Filtering;

public class CollectionScopeExpressionBuilderTests
{
    [Fact]
    public void Apply_WhenPathMatches_FiltersCollection()
    {
        Expression<Func<Storage, IEnumerable<Item>>> selector =
            source => source.Items;
        Expression<Func<Item, bool>> predicate =
            item => item.IsActive;

        var scopes = new[]
        {
            new RqlCollectionScope(selector, predicate)
        };
        var source = CreateStorage();

        var expression = CollectionScopeExpressionBuilder.Apply(
            selector.Body,
            selector.Parameters[0],
            scopes);

        var result = Execute(expression, selector.Parameters[0], source);

        result.Select(item => item.Id).Should().Equal(2, 3);
        source.Items.Select(item => item.Id).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Apply_WhenMultipleRulesMatch_AppliesAllRules()
    {
        Expression<Func<Storage, IEnumerable<Item>>> selector =
            source => source.Items;
        Expression<Func<Item, bool>> activePredicate =
            item => item.IsActive;
        Expression<Func<Item, bool>> tenantPredicate =
            item => item.TenantId == 7;

        var scopes = new[]
        {
            new RqlCollectionScope(selector, activePredicate),
            new RqlCollectionScope(selector, tenantPredicate)
        };
        var source = CreateStorage();

        var expression = CollectionScopeExpressionBuilder.Apply(
            selector.Body,
            selector.Parameters[0],
            scopes);

        var result = Execute(expression, selector.Parameters[0], source);

        result.Select(item => item.Id).Should().Equal(2);
    }

    [Fact]
    public void Apply_WhenPathDoesNotMatch_ReturnsOriginalExpression()
    {
        Expression<Func<Storage, IEnumerable<Item>>> selector =
            source => source.Items;
        Expression<Func<Storage, IEnumerable<Item>>> otherSelector =
            source => source.OtherItems;
        Expression<Func<Item, bool>> predicate =
            item => item.IsActive;

        var scopes = new[]
        {
            new RqlCollectionScope(otherSelector, predicate)
        };

        var result = CollectionScopeExpressionBuilder.Apply(
            selector.Body,
            selector.Parameters[0],
            scopes);

        result.Should().BeSameAs(selector.Body);
    }

    [Fact]
    public void Apply_WhenNestedPathMatches_FiltersCollection()
    {
        Expression<Func<Storage, IEnumerable<Item>>> selector =
            source => source.Account.Items;
        Expression<Func<Item, bool>> predicate =
            item => item.IsActive;

        var scopes = new[]
        {
            new RqlCollectionScope(selector, predicate)
        };
        var source = CreateStorage();

        var expression = CollectionScopeExpressionBuilder.Apply(
            selector.Body,
            selector.Parameters[0],
            scopes);

        var result = Execute(expression, selector.Parameters[0], source);

        result.Select(item => item.Id).Should().Equal(2, 3);
    }

    [Fact]
    public void Apply_WhenNestedParentDiffers_ReturnsOriginalExpression()
    {
        Expression<Func<Storage, IEnumerable<Item>>> selector =
            source => source.Account.Items;
        Expression<Func<Storage, IEnumerable<Item>>> otherSelector =
            source => source.OtherAccount.Items;
        Expression<Func<Item, bool>> predicate =
            item => item.IsActive;

        var scopes = new[]
        {
            new RqlCollectionScope(otherSelector, predicate)
        };

        var result = CollectionScopeExpressionBuilder.Apply(
            selector.Body,
            selector.Parameters[0],
            scopes);

        result.Should().BeSameAs(selector.Body);
    }

    [Fact]
    public void Apply_WhenExpressionUsesDifferentParameter_DoesNotMatch()
    {
        Expression<Func<Storage, IEnumerable<Item>>> selector =
            source => source.Items;
        Expression<Func<Item, bool>> predicate =
            item => item.IsActive;

        var otherRoot = Expression.Parameter(typeof(Storage), "other");
        var otherCollection = Expression.Property(
            otherRoot,
            nameof(Storage.Items));

        var scopes = new[]
        {
            new RqlCollectionScope(selector, predicate)
        };

        var result = CollectionScopeExpressionBuilder.Apply(
            otherCollection,
            selector.Parameters[0],
            scopes);

        result.Should().BeSameAs(otherCollection);
    }

    [Fact]
    public void Apply_WhenNoRulesExist_ReturnsOriginalExpression()
    {
        Expression<Func<Storage, IEnumerable<Item>>> selector =
            source => source.Items;

        var result = CollectionScopeExpressionBuilder.Apply(
            selector.Body,
            selector.Parameters[0],
            Array.Empty<RqlCollectionScope>());

        result.Should().BeSameAs(selector.Body);
    }

    private static List<Item> Execute(
        Expression expression,
        ParameterExpression parameter,
        Storage source)
    {
        var selector = Expression
            .Lambda<Func<Storage, IEnumerable<Item>>>(expression, parameter)
            .Compile();

        return selector(source).ToList();
    }

    private static Storage CreateStorage()
    {
        var items = new List<Item>
        {
            new() { Id = 1, IsActive = false, TenantId = 7 },
            new() { Id = 2, IsActive = true, TenantId = 7 },
            new() { Id = 3, IsActive = true, TenantId = 8 }
        };

        return new Storage
        {
            Items = items,
            Account = new Account { Items = items }
        };
    }

    public sealed class Storage
    {
        public List<Item> Items { get; set; } = [];
        public List<Item> OtherItems { get; set; } = [];
        public Account Account { get; set; } = new();
        public Account OtherAccount { get; set; } = new();
    }

    public sealed class Account
    {
        public List<Item> Items { get; set; } = [];
    }

    public sealed class Item
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public int TenantId { get; set; }
    }
}