using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class PropertyToPropertyFilterTests
{
    private readonly IRqlQueryable<Product, Product> _rql;

    public PropertyToPropertyFilterTests()
    {
        _rql = RqlFactory.Make<Product>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.Primitive;
            rql.Settings.Select.MaxDepth = 10;
        });
    }

    [Fact]
    public void Eq_PropertyToProperty_Equal()
    {
        // Arrange - Create explicit test data where Price == SellPrice for some products
        var testData = new List<Product>
        {
            new() { Id = 1, Name = "Product A", Category = "Test", Price = 100M, SellPrice = 100M, Reference = new Product() }, // Match
            new() { Id = 2, Name = "Product B", Category = "Test", Price = 200M, SellPrice = 150M, Reference = new Product() }, // No match  
            new() { Id = 3, Name = "Product C", Category = "Test", Price = 50M, SellPrice = 50M, Reference = new Product() }    // Match
        }.AsQueryable();

        // Act - Query for products where Price equals SellPrice
        var result = _rql.Transform(testData, new RqlRequest { Filter = "eq(price,sellPrice)" });

        // Assert - Should match products 1 and 3
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.Contains(products, p => p.Id == 1);
        Assert.Contains(products, p => p.Id == 3);
    }

    [Fact]
    public void Ne_PropertyToProperty_NotEqual()
    {
        // Arrange - Create explicit test data where Price != SellPrice for some products
        var testData = new List<Product>
        {
            new() { Id = 1, Name = "Product A", Category = "Test", Price = 100M, SellPrice = 100M, Reference = new Product() }, // No match
            new() { Id = 2, Name = "Product B", Category = "Test", Price = 200M, SellPrice = 150M, Reference = new Product() }, // Match
            new() { Id = 3, Name = "Product C", Category = "Test", Price = 50M, SellPrice = 75M, Reference = new Product() }    // Match
        }.AsQueryable();

        // Act - Query for products where Price does not equal SellPrice  
        var result = _rql.Transform(testData, new RqlRequest { Filter = "ne(price,sellPrice)" });

        // Assert - Should match products 2 and 3
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.Contains(products, p => p.Id == 2);
        Assert.Contains(products, p => p.Id == 3);
    }

    [Fact]
    public void Gt_PropertyToProperty_GreaterThan()
    {
        // Arrange - Create explicit test data
        var testData = new List<Product>
        {
            new() { Id = 1, Name = "Product A", Category = "Test", Price = 100M, SellPrice = 150M, Reference = new Product() }, // No match: 100 > 150 is false
            new() { Id = 2, Name = "Product B", Category = "Test", Price = 200M, SellPrice = 150M, Reference = new Product() }, // Match: 200 > 150
            new() { Id = 3, Name = "Product C", Category = "Test", Price = 50M, SellPrice = 50M, Reference = new Product() }    // No match: 50 > 50 is false
        }.AsQueryable();

        // Act - Query for products where Price > SellPrice
        var result = _rql.Transform(testData, new RqlRequest { Filter = "gt(price,sellPrice)" });

        // Assert - Should match only product 2
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal(2, products[0].Id);
    }

    [Fact]
    public void Eq_ConstantValue_StillWorks()
    {
        // Arrange - Verify that constant comparisons still work (not property-to-property)
        var testData = new List<Product>
        {
            new() { Id = 1, Name = "Jewelry Widget", Category = "Test", Price = 100M, SellPrice = 100M, Reference = new Product() }, // Match
            new() { Id = 2, Name = "Product B", Category = "Test", Price = 200M, SellPrice = 150M, Reference = new Product() },
            new() { Id = 3, Name = "Product C", Category = "Test", Price = 300M, SellPrice = 250M, Reference = new Product() }
        }.AsQueryable();

        // Act - Use constant string comparison
        var result = _rql.Transform(testData, new RqlRequest { Filter = "eq(name,Jewelry Widget)" });

        // Assert - Should match only product 1
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal(1, products[0].Id);
        Assert.Equal("Jewelry Widget", products[0].Name);
    }

    [Fact]
    public void Gt_ConstantValue_StillWorks()
    {
        // Arrange - Verify constant numeric comparisons still work
        var testData = new List<Product>
        {
            new() { Id = 1, Name = "Product A", Category = "Test", Price = 100M, SellPrice = 50M, Reference = new Product() },
            new() { Id = 2, Name = "Product B", Category = "Test", Price = 250M, SellPrice = 150M, Reference = new Product() }, // Match: 250 > 200.5
            new() { Id = 3, Name = "Product C", Category = "Test", Price = 300M, SellPrice = 250M, Reference = new Product() }  // Match: 300 > 200.5
        }.AsQueryable();

        // Act - Use constant numeric comparison
        var result = _rql.Transform(testData, new RqlRequest { Filter = "gt(price,200.5)" });

        // Assert - Should match products 2 and 3
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.Contains(products, p => p.Id == 2);
        Assert.Contains(products, p => p.Id == 3);
    }

    [Fact]
    public void Eq_QuotedTextLooksLikeProperty_TreatedAsConstant()
    {
        // Arrange - Verify that quoted text matching a property name is treated as a constant string, not a property
        var testData = new List<Product>
        {
            new() { Id = 1, Name = "SellPrice", Category = "Test", Price = 100M, SellPrice = 100M, Reference = new Product() }, // Match: name literally equals "SellPrice"
            new() { Id = 2, Name = "Product B", Category = "Test", Price = 200M, SellPrice = 150M, Reference = new Product() }, // No match
            new() { Id = 3, Name = "Price", Category = "Test", Price = 50M, SellPrice = 50M, Reference = new Product() }        // No match
        }.AsQueryable();

        // Act - Use quoted string 'SellPrice' which matches a property name but should be treated as constant
        var result = _rql.Transform(testData, new RqlRequest { Filter = "eq(name,'SellPrice')" });

        // Assert - Should match only product 1 where name literally equals "SellPrice"
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal(1, products[0].Id);
        Assert.Equal("SellPrice", products[0].Name);
    }
}
