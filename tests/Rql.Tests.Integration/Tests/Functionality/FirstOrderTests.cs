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
    private static IRqlQueryable<Product, Product> Make(NavigationStrategy navigation = NavigationStrategy.Default) =>
        RqlFactory.Make<Product>(services => { }, rql =>
        {
            // Transparent mapping skips the projection step, so inline data needs no unrelated navigations.
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Ordering.Navigation = navigation;
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
    public void NullCollection_SafeNavigation_YieldsNullKey()
    {
        var data = new List<Product> { new() { Id = 9, Name = "Z", Category = "X", Orders = null! } }.Concat(Data()).AsQueryable();

        var result = Make(NavigationStrategy.Safe).Transform(data, new RqlRequest { Order = "+first(orders,eq(clientName,Michael),id)" });

        Assert.Equal([9, 4, 5, 2, 3, 1], Ids(result));
    }

    // ── Errors ────────────────────────────────────────────────────────────────

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
