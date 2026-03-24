using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Integration tests for the built-in <c>orderby()</c> ordering function.
/// Syntax: <c>+orderby(collectionProperty,filterPropertyName,filterValue,resultPropertyName)</c>
/// Finds the first element in the collection matching the filter and uses its property as the sort key.
/// </summary>
public class OrderByOrderTests
{
    private readonly IRqlQueryable<Product, Product> _rql;

    public OrderByOrderTests()
    {
        _rql = RqlFactory.Make<Product>(services => { }, rql =>
        {
            // Transparent mapping skips the TStorage→TView projection step (same type, no mapping needed).
            // This lets tests use minimal inline data without initialising unrelated navigation properties.
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a dataset where products have distinct "Michael"-order ids so the exact sort
    /// sequence can be verified, plus two null-key products.
    /// <para>
    /// Layout:
    ///   Id=1 → Michael order id=30  (highest key)
    ///   Id=2 → Michael order id=10  (lowest key)
    ///   Id=3 → Michael order id=20  (middle key)
    ///   Id=4 → Tony order only       (null key — no Michael match)
    ///   Id=5 → no orders             (null key)
    /// </para>
    /// </summary>
    private static IQueryable<Product> MakeOrderByData() => new List<Product>
    {
        new() { Id = 1, Name = "A", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                Orders = [new ProductOrder { Id = 30, ClientName = "Michael" }], OrdersIds = [30], Tags = [] },
        new() { Id = 2, Name = "B", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                Orders = [new ProductOrder { Id = 10, ClientName = "Michael" }], OrdersIds = [10], Tags = [] },
        new() { Id = 3, Name = "C", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                Orders = [new ProductOrder { Id = 20, ClientName = "Michael" }], OrdersIds = [20], Tags = [] },
        new() { Id = 4, Name = "D", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                Orders = [new ProductOrder { Id = 99, ClientName = "Tony" }], OrdersIds = [99], Tags = [] },
        new() { Id = 5, Name = "E", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                Orders = [], OrdersIds = [], Tags = [] },
    }.AsQueryable();

    // ── Exact sort-order tests (custom data with distinct keys) ──────────────

    [Fact]
    public void OrderBy_IntResult_Ascending_ExactOrder()
    {
        // null keys first (products 4, 5), then ascending by matched order id: 10→id=2, 20→id=3, 30→id=1
        var result = _rql.Transform(MakeOrderByData(), new RqlRequest
        {
            Order = "+orderby(orders,clientName,Michael,id)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(p => p.Id).ToList();
        Assert.Equal([4, 5, 2, 3, 1], ids);
    }

    [Fact]
    public void OrderBy_IntResult_Descending_ExactOrder()
    {
        // Descending: 30→id=1, 20→id=3, 10→id=2, then nulls (4, 5)
        var result = _rql.Transform(MakeOrderByData(), new RqlRequest
        {
            Order = "-orderby(orders,clientName,Michael,id)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(p => p.Id).ToList();
        Assert.Equal([1, 3, 2, 4, 5], ids);
    }

    [Fact]
    public void OrderBy_NullKeys_SortBeforeNonNull_Ascending()
    {
        // Explicitly verify null keys come first in ascending sort
        var result = _rql.Transform(MakeOrderByData(), new RqlRequest
        {
            Order = "+orderby(orders,clientName,Michael,id)"
        });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();

        var nullKeyProducts = products.TakeWhile(p => !p.Orders.Any(o => o.ClientName == "Michael")).ToList();
        Assert.Equal(2, nullKeyProducts.Count);

        var nonNullKeyProducts = products.Skip(2).ToList();
        Assert.All(nonNullKeyProducts, p => Assert.Contains(p.Orders, o => o.ClientName == "Michael"));
    }

    [Fact]
    public void OrderBy_NullKeys_SortAfterNonNull_Descending()
    {
        var result = _rql.Transform(MakeOrderByData(), new RqlRequest
        {
            Order = "-orderby(orders,clientName,Michael,id)"
        });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();

        var nonNullFirst = products.TakeWhile(p => p.Orders.Any(o => o.ClientName == "Michael")).ToList();
        Assert.Equal(3, nonNullFirst.Count);

        var nullLast = products.Skip(3).ToList();
        Assert.All(nullLast, p => Assert.DoesNotContain(p.Orders, o => o.ClientName == "Michael"));
    }

    [Fact]
    public void OrderBy_UsesFirstMatch_NotMinOrMax()
    {
        // Product 1 has two Michael orders: id=99 first, id=1 second.
        // orderby must use the FIRST matching order (id=99), not the minimum (id=1).
        var data = new List<Product>
        {
            new() { Id = 1, Name = "MultiMatch", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                    Orders = [
                        new ProductOrder { Id = 99, ClientName = "Michael" },   // first match
                        new ProductOrder { Id = 1,  ClientName = "Michael" },   // should be ignored
                    ],
                    OrdersIds = [99, 1], Tags = [] },
            new() { Id = 2, Name = "SingleMatch", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                    Orders = [new ProductOrder { Id = 50, ClientName = "Michael" }],
                    OrdersIds = [50], Tags = [] },
        }.AsQueryable();

        var result = _rql.Transform(data, new RqlRequest
        {
            Order = "+orderby(orders,clientName,Michael,id)"
        });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();

        // Product 2 key=50, Product 1 key=99 (first match) → Product 2 comes first ascending
        // If min was used, Product 1 (min id=1) would come first — this test catches that bug
        Assert.Equal(2, products[0].Id);
        Assert.Equal(1, products[1].Id);
    }

    [Fact]
    public void OrderBy_EmptyCollection_TreatedAsNullKey_SortsFirst_Ascending()
    {
        var data = new List<Product>
        {
            new() { Id = 1, Name = "A", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                    Orders = [new ProductOrder { Id = 5, ClientName = "Michael" }],
                    OrdersIds = [5], Tags = [] },
            new() { Id = 2, Name = "B", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                    Orders = [], OrdersIds = [], Tags = [] },  // empty collection → null key
        }.AsQueryable();

        var result = _rql.Transform(data, new RqlRequest
        {
            Order = "+orderby(orders,clientName,Michael,id)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(p => p.Id).ToList();
        Assert.Equal([2, 1], ids);  // empty-collection product sorts first
    }

    [Fact]
    public void OrderBy_IntFilterValue_Ascending()
    {
        // Filter on an int property (orders.id), result is a string property
        // Only Product 2 has an order with id=10 → non-null key, comes last ascending
        var result = _rql.Transform(MakeOrderByData(), new RqlRequest
        {
            Order = "+orderby(orders,id,10,clientName)"
        });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();

        Assert.Equal(2, products.Last().Id);
        Assert.Equal("Michael", products.Last().Orders.First(o => o.Id == 10).ClientName);
    }

    [Fact]
    public void OrderBy_StringResult_Ascending_AlphabeticOrder()
    {
        var data = new List<Product>
        {
            new() { Id = 1, Name = "A", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                    Orders = [new ProductOrder { Id = 1, ClientName = "Zara" }], OrdersIds = [1], Tags = [] },
            new() { Id = 2, Name = "B", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                    Orders = [new ProductOrder { Id = 1, ClientName = "Alice" }], OrdersIds = [1], Tags = [] },
            new() { Id = 3, Name = "C", Category = "X", Price = 1, SellPrice = 1, ListDate = DateTime.Now,
                    Orders = [new ProductOrder { Id = 1, ClientName = "Mark" }], OrdersIds = [1], Tags = [] },
        }.AsQueryable();

        var result = _rql.Transform(data, new RqlRequest
        {
            Order = "+orderby(orders,id,1,clientName)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(p => p.Id).ToList();
        // Alice(2) < Mark(3) < Zara(1)
        Assert.Equal([2, 3, 1], ids);
    }

    [Fact]
    public void OrderBy_CombinedWithScalarSort_TieBreaking()
    {
        // Products 4 and 5 both have null keys; secondary +id should order them 4, 5
        var result = _rql.Transform(MakeOrderByData(), new RqlRequest
        {
            Order = "+orderby(orders,clientName,Michael,id),+id"
        });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();

        // Null-key group comes first, ordered by id
        var nullKeyIds = products.Take(2).Select(p => p.Id).ToList();
        Assert.Equal([4, 5], nullKeyIds);

        // Non-null group sorted by matched order id: 10→id=2, 20→id=3, 30→id=1
        var nonNullIds = products.Skip(2).Select(p => p.Id).ToList();
        Assert.Equal([2, 3, 1], nonNullIds);
    }

    // ── Error cases ──────────────────────────────────────────────────────────

    [Fact]
    public void OrderBy_UnknownFunction_ReturnsError()
    {
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest
        {
            Order = "+unknownFunc(orders,id,1,clientName)"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Unknown ordering function"));
    }

    [Fact]
    public void OrderBy_WrongArgCount_ReturnsError()
    {
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest
        {
            Order = "+orderby(orders,clientName,Michael)"  // 3 args, needs 4
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("4 arguments"));
    }

    [Fact]
    public void OrderBy_NonCollectionProperty_ReturnsError()
    {
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest
        {
            Order = "+orderby(name,anything,val,anything)"  // "name" is string, not a collection
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("not a collection property"));
    }

    [Fact]
    public void OrderBy_InvalidFilterProperty_ReturnsError()
    {
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest
        {
            Order = "+orderby(orders,nonExistent,val,clientName)"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Invalid property path."));
    }

    [Fact]
    public void OrderBy_InvalidResultProperty_ReturnsError()
    {
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest
        {
            Order = "+orderby(orders,clientName,Michael,nonExistent)"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Invalid property path."));
    }

    [Fact]
    public void OrderBy_IncompatibleFilterValueType_ReturnsError()
    {
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest
        {
            Order = "+orderby(orders,id,not-a-number,clientName)"  // orders.id is int
        });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Cannot convert"));
    }
}
