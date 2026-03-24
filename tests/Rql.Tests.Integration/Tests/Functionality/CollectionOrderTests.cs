using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Tests for sorting by a scalar property of an inner collection element, e.g. sort(+orders.clientName).
/// The first element of the collection is used as the sort key (Select + FirstOrDefault semantics).
/// </summary>
public class CollectionOrderTests
{
    private static readonly string[] TagsAscending = ["Tag1", "Tag2", "Tag3", "Tag4", "Tag5", "Tag6", "Tag7", "Tag8"];
    private static readonly string[] TagsDescending = ["Tag8", "Tag7", "Tag6", "Tag5", "Tag4", "Tag3", "Tag2", "Tag1"];

    private readonly IRqlQueryable<Product, Product> _rql;

    public CollectionOrderTests()
    {
        _rql = RqlFactory.Make<Product>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });
    }

    [Fact]
    public void Ordering_ByCollectionInnerStringProperty_Ascending_NullsFirst()
    {
        // Products with no orders have a null first clientName and sort before those with orders
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "+orders.clientName" });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(8, products.Count);

        // Products 2,3,4,5,6,8 have empty Orders → null clientName → sort first (ascending)
        var first6 = products.Take(6).ToList();
        Assert.All(first6, p => Assert.Empty(p.Orders));

        // Products 1 and 7 have orders with clientName "Michael" → come last
        var last2 = products.Skip(6).ToList();
        Assert.All(last2, p => Assert.Equal("Michael", p.Orders.First().ClientName));
    }

    [Fact]
    public void Ordering_ByCollectionInnerStringProperty_Descending_NullsLast()
    {
        // Descending: non-null clientNames come first, null (empty orders) come last
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "-orders.clientName" });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(8, products.Count);

        // Products with orders come first when descending
        var first2 = products.Take(2).ToList();
        Assert.All(first2, p => Assert.Equal("Michael", p.Orders.First().ClientName));

        // Products with empty orders (null clientName) come last
        var last6 = products.Skip(2).ToList();
        Assert.All(last6, p => Assert.Empty(p.Orders));
    }

    [Fact]
    public void Ordering_ByCollectionInnerIntProperty_Ascending_NullsFirst()
    {
        // int property: value types are made nullable so empty-collection entries sort as null (first in asc)
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "+orders.id" });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(8, products.Count);

        // Products with empty orders (null first order id) sort before products with orders
        var emptyOrderProducts = products.TakeWhile(p => p.Orders.Count == 0).ToList();
        Assert.NotEmpty(emptyOrderProducts);
        Assert.All(emptyOrderProducts, p => Assert.Empty(p.Orders));

        // At least one product with an order follows
        Assert.Contains(products, p => p.Orders.Count > 0);
    }

    [Fact]
    public void Ordering_ByCollectionInnerProperty_DistinctValues_SortsCorrectly()
    {
        // Each product has exactly one tag with a unique value: "Tag1" through "Tag8"
        // Ascending sort should yield Tag1, Tag2, ..., Tag8 order
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "+tags.value" });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(8, products.Count);

        // Tags are "Tag1".."Tag8"; string sort: Tag1 < Tag2 < ... < Tag8
        var tagValues = products.Select(p => p.Tags.First().Value).ToList();
        Assert.Equal(TagsAscending, tagValues);
    }

    [Fact]
    public void Ordering_ByCollectionInnerProperty_Descending_DistinctValues_SortsCorrectly()
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "-tags.value" });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();

        var tagValues = products.Select(p => p.Tags.First().Value).ToList();
        Assert.Equal(TagsDescending, tagValues);
    }

    [Fact]
    public void Ordering_ByCollectionInnerProperty_InvalidSubPath_ReturnsError()
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "+orders.nonExistentField" });

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid property path.", result.Errors.First().Message);
    }

    [Fact]
    public void Ordering_ByCollectionInnerProperty_CombinedWithScalarOrder_WorksCorrectly()
    {
        // Combine collection-inner sort with a regular scalar sort
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "+orders.clientName,+id" });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(8, products.Count);

        // Primary key: orders.clientName (null-first)
        // Secondary key: id (ascending)
        // Empty-orders products come first, ordered by id
        var emptyOrdersProducts = products.TakeWhile(p => p.Orders.Count == 0).ToList();
        Assert.NotEmpty(emptyOrdersProducts);
        var ids = emptyOrdersProducts.Select(p => p.Id).ToList();
        Assert.Equal(ids.OrderBy(x => x).ToList(), ids); // secondary sort by id is ascending
    }
}
