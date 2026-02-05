using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicFilterTests
{
    private readonly IRqlQueryable<Product, Product> _rql;

    public BasicFilterTests()
    {
        _rql = RqlFactory.Make<Product>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
            rql.Settings.Filter.Navigation = Mpt.Rql.Abstractions.Configuration.NavigationStrategy.Safe;
            rql.Settings.Ordering.Navigation = Mpt.Rql.Abstractions.Configuration.NavigationStrategy.Safe;
        });
    }

    private static List<Product> GetTestData() => [.. new List<Product>()
    {
        new() { Id = 1, Name = "Jewelry Widget", Category = "Clothing", Price = 192.95M, SellPrice = 172.99M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc1",
            Orders = [
                new ProductOrder() { Id = 1, ClientName = "Michael" },
                new ProductOrder() { Id = 2, ClientName = "Tony" },
                new ProductOrder() { Id = 3, ClientName = "Isabel" },
            ],
            OrdersIds = [1, 2, 3]
        },
        new() { Id = 2, Name = "Camping Whatchamacallit", Category = "Activity", Price = 95M, SellPrice = 74.99M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc2",
            Orders = [
                new ProductOrder() { Id = 2, ClientName = "Bob" }
            ],
            OrdersIds = [2]
        },
        new() { Id = 3, Name = "Sports Contraption", Category = "Activity", Price = 820.95M, SellPrice = 64M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc3",
            Orders = [
                new ProductOrder() { Id = 3, ClientName = "Carol" }
            ],
            OrdersIds = [3]
        },
        new() { Id = 4, Name = "Furniture Apparatus", Category = "Home", Price = 146M, SellPrice = 50M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc4",
            Orders = [
                new ProductOrder() { Id = 4, ClientName = "Dave" }
            ],
            OrdersIds = [4]
        },
        new() { Id = 5, Name = "Dog Whatchamacallit", Category = "Pets", Price = 205.15M, SellPrice = 3M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc5",
            Orders = [
                new ProductOrder() { Id = 5, ClientName = "Eve" }
            ],
            OrdersIds = [5]
        },
        new() { Id = 6, Name = "Makeup Contraption", Category = "", Price = 129.99M, SellPrice = 129.99M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!,
            Desc = null,
            Orders = [
                new ProductOrder() { Id = 6, ClientName = "Frank" }
            ],
            OrdersIds = [6]
        },
        new() { Id = 7, Name = "Bath Contraption", Category = "Beauty", Price = 106.99M, SellPrice = 84.95M, ListDate = DateTime.Now,
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc7",
            Orders = [
                new ProductOrder() { Id = 1, ClientName = "Michael" }
            ],
            OrdersIds = [1]
        },
        new() { Id = 8, Name = "*Example \\With*", Category = "Activity", Price = 450.95M, SellPrice = 76M, ListDate = DateTime.Now.AddDays(-7),
            Reference = null!, Collection = null!, Tags = null!, Desc = "Desc8",
            Orders = [
                new ProductOrder() { Id = 8, ClientName = "Grace" }
            ],
            OrdersIds = [8]
        }
    }.Select(p =>
    {
        p.Reference = p;
        p.Collection = [p, p];
        p.Tags = [new Tag() { Value = $"Tag{p.Id}" }];
        return p;
    })];

    [Theory]
    [InlineData("eq(name,Jewelry Widget)")]
    [InlineData("name=Jewelry Widget")]
    public void Eq_Name_Equal(string query)
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
    [InlineData("eq(name,WRONG_DATA)")]
    public void Eq_Name_Equal_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }


    [Theory]
    [InlineData("eq(reference.name,Jewelry Widget)")]
    [InlineData("reference.name=Jewelry Widget")]
    public void Path_Name_Equal(string query)
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
    [InlineData("eq(reference.name,WRONG_DATA)")]
    public void Path_Name_Equal_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("ne(name,Jewelry Widget)")]
    public void Ne_Name_NotEqual(string query)
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
    [InlineData("ne(name,WRONG_DATA)")]
    public void Ne_Name_NotEqual_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(8, products.Count); // All products since WRONG_DATA doesn't exist
    }

    [Theory]
    [InlineData("gt(price,200.5)")]
    public void Gt_Price_GreaterThan(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(3, products.Count);
        Assert.All(products, p => Assert.True(p.Price > 200.5M));
    }

    [Theory]
    [InlineData("gt(price,10000)")]
    public void Gt_Price_GreaterThan_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("ge(price,129.99)")]
    public void Ge_Price_GreaterThanOrEqual(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(6, products.Count);
        Assert.All(products, p => Assert.True(p.Price >= 129.99M));
    }

    [Theory]
    [InlineData("ge(price,10000)")]
    public void Ge_Price_GreaterThanOrEqual_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("lt(price,150.1)")]
    public void Lt_Price_LessThan(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(4, products.Count);
        Assert.All(products, p => Assert.True(p.Price < 150.1M));
    }

    [Theory]
    [InlineData("lt(price,-10000)")]
    public void Lt_Price_LessThan_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("lt(self(price),150.1)")]
    public void Lt_Price_With_Self_LessThan(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(4, products.Count);
        Assert.All(products, p => Assert.True(p.Price < 150.1M));
    }

    [Theory]
    [InlineData("lt(self(price),-10000)")]
    public void Lt_Price_With_Self_LessThan_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("le(price,205.15)")]
    public void Le_Price_LessThanOrEqual(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(6, products.Count);
        Assert.All(products, p => Assert.True(p.Price <= 205.15M));
    }

    [Theory]
    [InlineData("le(price,-1000)")]
    public void Le_Price_LessThanOrEqual_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("like(name,Jewelry*)", "Jewelry Widget")]
    [InlineData("ilike(name,'*Jewelry*')", "Jewelry Widget")]
    [InlineData("(ilike(name,'*ewelr*')|ilike(name,'*Jewelry*'))", "Jewelry Widget")]
    [InlineData("like(name,*With*)", "*Example \\With*")]
    [InlineData(@"like(name,*With\*)", "*Example \\With*")]
    [InlineData(@"like(name,\*Example \With\*)", "*Example \\With*")]
    [InlineData(@"like(name,*Example \With*)", "*Example \\With*")]
    [InlineData(@"like(name,*Example*)", "*Example \\With*")]
    [InlineData(@"like(name,*\With*)", "*Example \\With*")]
    [InlineData(@"like(name,*\With\*)", "*Example \\With*")]
    public void Like_Name_Matches(string query, string expectedName)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal(expectedName, products[0].Name);
    }

    [Theory]
    [InlineData("like(name,WRONG_DATA*)")]
    [InlineData(@"like(name,*Jewelry\*)")]
    [InlineData(@"like(name,*With\**)")]
    [InlineData(@"like(name,With\*)")]
    [InlineData(@"like(name,*\\\Example*)")]
    [InlineData(@"like(name,Example*)")]
    public void Like_Name_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("like(name,*Widget)")]
    public void Like_Name_EndsWith(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.EndsWith("Widget", products[0].Name);
    }

    [Theory]
    [InlineData("like(name,*WRONG_DATA)")]
    public void Like_Name_EndsWith_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("like(name,*Wid*)")]
    public void Like_Name_Contains(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Contains("Wid", products[0].Name);
    }

    [Theory]
    [InlineData("like(name,*WRONG_DATA*)")]
    public void Like_Name_Contains_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("not(eq(id,1))")]
    public void Not_Name_NotContains(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(7, products.Count);
        Assert.DoesNotContain(products, p => p.Id == 1);
    }

    [Theory]
    [InlineData("not(eq(id,2))")]
    public void Not_Name_NotContains_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(7, products.Count);
        Assert.DoesNotContain(products, p => p.Id == 2);
    }

    [Theory]
    [InlineData("in(id,(1,3,6))")]
    public void In_Id_MatchList(string query)
    {
        // Arrange
        var testData = GetTestData();
        var expectedIds = new List<int> { 1, 3, 6 };

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(3, products.Count);
        Assert.All(products, p => Assert.Contains(p.Id, expectedIds));
    }

    [Theory]
    [InlineData("in(id,(1,3))")]
    public void In_Id_MatchList_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();
        var expectedIds = new List<int> { 1, 3 };

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.All(products, p => Assert.Contains(p.Id, expectedIds));
    }

    [Theory]
    [InlineData("out(id,(1,3,6))")]
    public void Out_Id_NotMatchList(string query)
    {
        // Arrange
        var testData = GetTestData();
        var excludedIds = new List<int> { 1, 3, 6 };

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(5, products.Count);
        Assert.All(products, p => Assert.DoesNotContain(p.Id, excludedIds));
    }

    [Theory]
    [InlineData("out(id,(1,3,7))")]
    public void Out_Id_NotMatchList_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();
        var excludedIds = new List<int> { 1, 3, 7 };

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(5, products.Count);
        Assert.All(products, p => Assert.DoesNotContain(p.Id, excludedIds));
    }

    [Theory]
    [InlineData("desc=null()")]
    public void Null_Desc_DescriptionIsNull(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Null(products[0].Desc);
    }

    [Theory]
    [InlineData("not(eq(desc,null()))")]
    public void Null_Desc_DescriptionIsNull_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(7, products.Count);
        Assert.All(products, p => Assert.NotNull(p.Desc ?? ""));
    }

    [Theory]
    [InlineData("category=empty()")]
    public void Empty_Category_CategoryIsEmpty(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal(string.Empty, products[0].Category);
    }

    [Theory]
    [InlineData("not(eq(category,empty()))")]
    public void Empty_Category_CategoryIsEmpty_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(7, products.Count);
        Assert.All(products, p => Assert.NotEqual(string.Empty, p.Category));
    }

    [Theory]
    [InlineData("and(eq(id,1),eq(name,Jewelry Widget))")]
    [InlineData("and(id=1,name=Jewelry Widget)")]
    public void And_Id_Name_Equals(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.Equal(1, products[0].Id);
        Assert.Equal("Jewelry Widget", products[0].Name);
    }

    [Theory]
    [InlineData("and(eq(id,1),eq(id,2),eq(name,Jewelry Widget))")]
    public void And_Id_Name_Equals_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("or(eq(id,1),eq(id,2),eq(name,Jewelry Widget))")]
    [InlineData("or(id=1,id=2,name=Jewelry Widget)")]
    public void Or_Id_Name_Equals(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.Contains(products, p => p.Id == 1);
        Assert.Contains(products, p => p.Id == 2);
    }

    [Theory]
    [InlineData("or(eq(id,3),eq(id,5),eq(name,Jewelry Widget))")]
    public void Or_Id_Name_Equals_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(3, products.Count);
        Assert.Contains(products, p => p.Id == 1);
        Assert.Contains(products, p => p.Id == 3);
        Assert.Contains(products, p => p.Id == 5);
    }

    [Theory]
    [InlineData("any(orders,(id=1))")]
    [InlineData("any(orders,eq(id,1))")]
    public void Any_Orders_Id_Equals(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.All(products, p => Assert.Contains(p.Orders, o => o.Id == 1));
    }

    [Theory]
    [InlineData("any(orders,eq(id,999))")]
    public void Any_Orders_Id_Equals_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("any(orders,(id=1,id=1))")]
    [InlineData("any(orders,and(eq(id,1),eq(id,1)))")]
    public void Complex_Any_Orders_Id_Equals(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.All(products, p => Assert.Contains(p.Orders, o => o.Id == 1));
    }

    [Theory]
    [InlineData("any(orders,and(eq(id,999),eq(id,999)))")]
    public void Complex_Any_Orders_Id_Equals_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("any(orders)")]
    public void Any_Orders(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(8, products.Count);
        Assert.All(products, p => Assert.NotEmpty(p.Orders));
    }

    [Theory]
    [InlineData("all(orders,id=1)")]
    [InlineData("all(orders,eq(id,1))")]
    public void All_Orders_Id_Equals(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Single(products);
        Assert.All(products[0].Orders, o => Assert.Equal(1, o.Id));
    }

    [Theory]
    [InlineData("all(orders,eq(id,999))")]
    public void All_Orders_Id_Equals_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Theory]
    [InlineData("any(OrdersIds,self()=1)")]
    [InlineData("any(OrdersIds,eq(self(),1))")]
    public void Any_SaleDetailIds_Equals(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(2, products.Count);
        Assert.All(products, p => Assert.Contains(1, p.OrdersIds));
    }

    [Theory]
    [InlineData("any(OrdersIds,self()=222)")]
    public void Any_SaleDetailIds_Equals_NoMatch(string query)
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = query });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Empty(products);
    }

    [Fact]
    public void Equals_IsIgnored_ThrowsException()
    {
        // Arrange
        var testData = GetTestData();

        // Act
        var result = _rql.Transform(testData.AsQueryable(), new RqlRequest { Filter = "ignored=true" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid property path.", result.Errors.First().Message);
    }
}