using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class CaseInsensitiveFilterTests
{
    private readonly IRqlQueryable<Product, Product> _rql;

    public CaseInsensitiveFilterTests()
    {
        _rql = RqlFactory.Make<Product>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;

            // Enable case insensitive string comparisons
            rql.Settings.Filter.Strings.Comparison = StringComparison.OrdinalIgnoreCase;
            rql.Settings.Filter.Strings.Strategy = StringComparisonStrategy.Default;
        });
    }

    private static List<Product> GetTestData() => [.. new List<Product>()
    {
        new() { Id = 1, Name = "Jewelry Widget", Category = "Clothing", Price = 192.95M, SellPrice = 172.99M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc1",
            Orders = [
                new() { Id = 1, ClientName = "Michael" },
                new() { Id = 2, ClientName = "Tony" },
                new() { Id = 3, ClientName = "Isabel" },
            ],
            OrdersIds = [1, 2, 3]
        },
        new() { Id = 2, Name = "Camping Whatchamacallit", Category = "Activity", Price = 95M, SellPrice = 74.99M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc2",
            Orders = [
                new() { Id = 2, ClientName = "Bob" }
            ],
            OrdersIds = [2]
        },
        new() { Id = 3, Name = "Sports Contraption", Category = "Activity", Price = 820.95M, SellPrice = 64M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc3",
            Orders = [
                new() { Id = 3, ClientName = "Carol" }
            ],
            OrdersIds = [3]
        },
        new() { Id = 4, Name = "Furniture Apparatus", Category = "Home", Price = 146M, SellPrice = 50M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc4",
            Orders = [
                new() { Id = 4, ClientName = "Dave" }
            ],
            OrdersIds = [4]
        },
        new() { Id = 5, Name = "Dog Whatchamacallit", Category = "Pets", Price = 205.15M, SellPrice = 3M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc5",
            Orders = [
                new() { Id = 5, ClientName = "Eve" }
            ],
            OrdersIds = [5]
        },
        new() { Id = 6, Name = "Makeup Contraption", Category = "", Price = 129.99M, SellPrice = 129.99M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!,
            Desc = null,
            Orders = [
                new() { Id = 6, ClientName = "Frank" }
            ],
            OrdersIds = [6]
        },
        new() { Id = 7, Name = "Bath Contraption", Category = "Beauty", Price = 106.99M, SellPrice = 84.95M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc7",
            Orders = [
                new() { Id = 1, ClientName = "Michael" }
            ],
            OrdersIds = [1]
        },
        new() { Id = 8, Name = "*Example \\With*", Category = "Activity", Price = 450.95M, SellPrice = 76M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc8",
            Orders = [
                new() { Id = 8, ClientName = "Grace" }
            ],
            OrdersIds = [8]
        }
    }.Select(p =>
    {
        p.Reference = p;
        p.Collection = [p, p];
        p.Tags = [new() { Value = $"Tag{p.Id}" }];
        return p;
    })];

    [Theory]
    [InlineData("eq(name,jewelry widget)")]
    [InlineData("eq(name,JEWELRY WIDGET)")]
    [InlineData("eq(name,JeWeLrY wIdGeT)")]
    [InlineData("name=jewelry widget")]
    [InlineData("name=JEWELRY WIDGET")]
    public void Eq_Name_Equal_CaseInsensitive(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal("Jewelry Widget", products[0].Name);
    }

    [Theory]
    [InlineData("ne(name,jewelry widget)")]
    [InlineData("ne(name,JEWELRY WIDGET)")]
    [InlineData("ne(name,JeWeLrY wIdGeT)")]
    public void Ne_Name_NotEqual_CaseInsensitive(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(7, products.Count);
        Assert.DoesNotContain(products, p => p.Name == "Jewelry Widget");
    }

    [Theory]
    [InlineData("like(category,*clothing*)")]
    [InlineData("like(category,*CLOTHING*)")]
    [InlineData("like(category,*ClOtHiNg*)")]
    public void Like_Category_Clothing_CaseInsensitive(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal("Clothing", products[0].Category);
    }

    [Theory]
    [InlineData("like(category,*activity*)")]
    [InlineData("like(category,*ACTIVITY*)")]
    public void Like_Category_Activity_CaseInsensitive(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(3, products.Count);
        Assert.All(products, p => Assert.Equal("Activity", p.Category));
    }

    [Theory]
    [InlineData("like(name,*widget*)")]
    [InlineData("like(name,*WIDGET*)")]
    [InlineData("like(name,*WiDgEt*)")]
    public void Like_Name_Widget_CaseInsensitive(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal("Jewelry Widget", products[0].Name);
    }

    [Theory]
    [InlineData("like(name,*whatchamacallit*)")]
    [InlineData("like(name,*WHATCHAMACALLIT*)")]
    public void Like_Name_Whatchamacallit_CaseInsensitive(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.Contains(products, p => p.Name == "Camping Whatchamacallit");
        Assert.Contains(products, p => p.Name == "Dog Whatchamacallit");
    }

    [Theory]
    [InlineData("and(eq(name,jewelry widget),eq(category,clothing))")]
    [InlineData("and(name=JEWELRY WIDGET,category=CLOTHING)")]
    [InlineData("and(like(name,*jewelry*),like(category,*clothing*))")]
    public void Complex_And_CaseInsensitive(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal("Jewelry Widget", products[0].Name);
        Assert.Equal("Clothing", products[0].Category);
    }
}
