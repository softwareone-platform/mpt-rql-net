using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// <c>first(&lt;collection&gt;, [&lt;predicate&gt;,] &lt;path&gt;)</c> against Product.Orders, LINQ-to-Objects.
/// </summary>
public class FirstOrderTests
{
    private static IRqlQueryable<Product, Product> Make(
        NavigationStrategy orderingNavigation = NavigationStrategy.Default,
        NavigationStrategy filterNavigation = NavigationStrategy.Default) =>
        RqlFactory.Make<Product>(services => { }, rql =>
        {
            // Transparent mapping skips the projection step, so inline data needs no unrelated navigations.
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Ordering.Navigation = orderingNavigation;
            rql.Settings.Filter.Navigation = filterNavigation;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });

    /// <summary>
    /// Id=1 → Michael order id 30 · Id=2 → Michael order id 10 · Id=3 → Michael order id 20 ·
    /// Id=4 → Tony order only (no Michael match) · Id=5 → no orders.
    /// </summary>
    private static IQueryable<Product> Data() => new List<Product>
    {
        new() { Id = 1, Name = "A", Category = "X", Orders = [new ProductOrder { Id = 30, ClientName = "Michael" }] },
        new() { Id = 2, Name = "B", Category = "X", Orders = [new ProductOrder { Id = 10, ClientName = "Michael" }] },
        new() { Id = 3, Name = "C", Category = "X", Orders = [new ProductOrder { Id = 20, ClientName = "Michael" }] },
        new() { Id = 4, Name = "D", Category = "X", Orders = [new ProductOrder { Id = 99, ClientName = "Tony" }] },
        new() { Id = 5, Name = "E", Category = "X", Orders = [] },
    }.AsQueryable();

    private static Product Michael(int productId, int orderId) =>
        new() { Id = productId, Name = "R", Category = "X", Orders = [new ProductOrder { Id = orderId, ClientName = "Michael" }] };

    private static List<int> Ids(RqlResponse<Product> result)
    {
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        return result.Query.Select(p => p.Id).ToList();
    }

    [Fact]
    public void Ascending_NullKeysFirst_ThenByMatchedValue()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void Descending_NullKeysLast()
        => Assert.Equal([1, 3, 2, 4, 5], Ids(Make().Transform(Data(), new RqlRequest { Order = "-first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void NoSign_IsAscending()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void QuotedPredicateValue_Works()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,'Michael'),id)" })));

    [Fact]
    public void TwoArguments_UsesFirstElement()
        // first order id: 1→30, 2→10, 3→20, 4→99, 5→null
        => Assert.Equal([5, 2, 3, 1, 4], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,id)" })));

    [Fact]
    public void CompoundPredicate_Works()
        // Michael AND id>15: 1→30, 2→null, 3→20, 4→null, 5→null
        => Assert.Equal([2, 4, 5, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,and(eq(clientName,Michael),gt(id,15)),id)" })));

    [Fact]
    public void InPredicate_Works()
        // Michael or Tony: 1→30, 2→10, 3→20, 4→99, 5→null
        => Assert.Equal([5, 2, 3, 1, 4], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,in(clientName,(Michael,Tony)),id)" })));

    [Fact]
    public void IntPredicateValue_StringResult()
    {
        var products = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(id,10),clientName)" }).Query.ToList();

        // only product 2 has an order with id 10 → the single non-null key sorts last ascending
        Assert.Equal(2, products.Last().Id);
    }

    [Fact]
    public void CombinedWithScalarSort_TieBreaksNullKeys()
        => Assert.Equal([4, 5, 2, 3, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id),+id" })));

    [Fact]
    public void ScalarThenFunction_Works()
        => Assert.Equal([5, 4, 3, 2, 1], Ids(Make().Transform(Data(), new RqlRequest { Order = "-id,+first(orders,eq(clientName,Michael),id)" })));

    [Fact]
    public void FirstMatch_NotMinOrMax_InMemorySemantics()
    {
        // In LINQ-to-Objects "first" is positional. A SQL provider gives no such guarantee — see the README caveat.
        var data = new List<Product>
        {
            new() { Id = 1, Name = "Multi", Category = "X", Orders = [new ProductOrder { Id = 99, ClientName = "Michael" }, new ProductOrder { Id = 1, ClientName = "Michael" }] },
            new() { Id = 2, Name = "Single", Category = "X", Orders = [new ProductOrder { Id = 50, ClientName = "Michael" }] },
        }.AsQueryable();

        Assert.Equal([2, 1], Ids(Make().Transform(data, new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id)" })));
    }

    [Fact]
    public void SafeNavigation_NullReferenceOnDottedCollectionPath_YieldsNullKey()
    {
        // The dotted prefix (reference) is guarded by the path builder under Safe navigation; the collection itself is not
        // (EF Core cannot translate a null check on a collection navigation).
        var data = new List<Product>
        {
            new() { Id = 1, Name = "A", Category = "X", Reference = Michael(10, 30) },
            new() { Id = 2, Name = "B", Category = "X", Reference = null! },
            new() { Id = 3, Name = "C", Category = "X", Reference = Michael(11, 10) },
        }.AsQueryable();

        var result = Make(NavigationStrategy.Safe).Transform(data, new RqlRequest { Order = "+first(reference.orders,eq(clientName,Michael),id)" });

        Assert.Equal([2, 3, 1], Ids(result));
    }

    [Fact]
    public void PredicateNavigation_FollowsTheOrderingSetting()
    {
        // Ordering=Safe, Filter=Default: the predicate's dotted path (reference.name) must still be null-safe,
        // because it is part of the ordering key.
        var data = new List<Product>
        {
            new() { Id = 1, Name = "A", Category = "X", Collection = [new Product { Id = 10, Name = "n", Category = "X", Reference = null! }] },
            new() { Id = 2, Name = "B", Category = "X", Collection = [new Product { Id = 20, Name = "n", Category = "X", Reference = new Product { Id = 99, Name = "x", Category = "X" } }] },
        }.AsQueryable();

        var result = Make(NavigationStrategy.Safe, NavigationStrategy.Default)
            .Transform(data, new RqlRequest { Order = "+first(collection,eq(reference.name,x),id)" });

        Assert.Equal([1, 2], Ids(result)); // product 1 has no match → null key first; no NullReferenceException
    }

    [Fact]
    public void SignOnlyGroup_SortsByItsItems()
        // "+(id,name)" is a plain list of order terms, not a function call
        => Assert.Equal([1, 2, 3, 4, 5], Ids(Make().Transform(Data(), new RqlRequest { Order = "+(id,name)" })));

    [Fact]
    public void UnquotedLiteralMatchingIncompatibleProperty_FallsBackToTheLiteral()
    {
        // `id` is also an element property (int); it cannot be coerced to clientName (string), so it is the literal "id".
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,id),id)" });

        Assert.Equal([1, 2, 3, 4, 5], Ids(result)); // nothing matches → all keys null → original order
    }

    // ── Error paths ────────────────────────────────────────────────────────────

    [Fact]
    public void UnknownFunction_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+nope(orders,eq(clientName,Michael),id)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "order:unknown_func" && e.Message == "Unknown ordering function 'nope'.");
    }

    [Fact]
    public void WrongArity_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "order:func_args");
    }

    [Fact]
    public void NonCollection_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(name,eq(clientName,Michael),id)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "order:not_collection" && e.Path == "name");
    }

    [Fact]
    public void MalformedPredicate_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName),id)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "order:malformed");
    }

    [Fact]
    public void UnknownPredicateProperty_ReportsPrefixedPath()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(nonExistent,x),id)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path." && e.Path == "orders.nonExistent");
    }

    [Fact]
    public void UnknownSelector_ReportsPrefixedPath()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(clientName,Michael),nonExistent)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path." && e.Path == "orders.nonExistent");
    }

    [Fact]
    public void UnknownSelector_MixedCaseDottedCollection_ReportsPrefixedPath()
    {
        var data = new List<Product> { new() { Id = 1, Name = "A", Category = "X", Reference = Michael(10, 30) } }.AsQueryable();

        var result = Make().Transform(data, new RqlRequest { Order = "+first(Reference.Orders,eq(clientName,Michael),nonExistent)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path." && e.Path == "reference.orders.nonExistent");
    }

    [Fact]
    public void UnknownSelector_AfterNestedAnyPredicate_ReportsPrefixedPath()
    {
        var data = new List<Product> { new() { Id = 1, Name = "A", Category = "X", Collection = [Michael(10, 30)] } }.AsQueryable();

        var result = Make().Transform(data, new RqlRequest { Order = "+first(collection,any(orders,eq(clientName,Michael)),nonExistent)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path." && e.Path == "collection.nonExistent");
    }

    [Fact]
    public void IncompatiblePredicateValue_IsAValidationError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+first(orders,eq(id,not-a-number),clientName)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Cannot convert value"));
    }

    // ── Regression: the dot-notation collection pivot from PR #27 is not part of this feature ──

    [Fact]
    public void DottedCollectionPath_InOrder_IsStillAnError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Order = "+orders.clientName" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path.");
    }

    [Fact]
    public void DottedCollectionPath_InFilter_IsStillAnError()
    {
        var result = Make().Transform(Data(), new RqlRequest { Filter = "eq(orders.clientName,Michael)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path.");
    }
}
