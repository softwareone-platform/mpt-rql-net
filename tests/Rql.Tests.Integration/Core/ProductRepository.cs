namespace Rql.Tests.Integration.Core;

public static class ProductRepository
{
    private static readonly List<Product> _data;

    static ProductRepository()
    {
        _data = new List<Product>
        {
            new Product { Id = 1, Name = "Jewelry Widget", Category = "Clothing", Price = 192.95M, SellPrice = 172.99M, ListDate = DateTime.Now,
                Orders = new List<ProductOrder> {
                    new ProductOrder { Id = 1, ClientName = "Michael" },
                    new ProductOrder { Id = 2, ClientName = "Tony" },
                    new ProductOrder { Id = 3, ClientName = "Isabel" },
                },
                OrdersIds = new List<int> { 1, 2 , 3 }
            },
            new Product { Id = 2, Name = "Camping Whatchamacallit", Category = "Activity", Price = 95, SellPrice = 74.99M , ListDate = DateTime.Now,
                Orders = new List<ProductOrder>{ },
                OrdersIds = new List<int> { }
            },
            new Product { Id = 3, Name = "Sports Contraption", Category = "Activity", Price = 820.95M, SellPrice = 64 , ListDate = DateTime.Now.AddDays(-7),
                Orders = new List<ProductOrder>{ },
                OrdersIds = new List<int> { }
            },
            new Product { Id = 4, Name = "Furniture Apparatus", Category = "Home", Price = 146, SellPrice = 50 , ListDate = DateTime.Now,
                Orders = new List<ProductOrder>{ },
                OrdersIds = new List<int> { }
            },
            new Product { Id = 5, Name = "Dog Whatchamacallit", Category = "Pets", Price = 205.15M, SellPrice = 3 , ListDate = DateTime.Now.AddDays(-7),
                Orders = new List<ProductOrder>{ },
                OrdersIds = new List<int> { }
            },
            new Product { Id = 6, Name = "Makeup Contraption", Category = "", Price = 129.99M, SellPrice = 129.99M , ListDate = DateTime.Now.AddDays(-7),
                Orders = new List<ProductOrder> { },
                OrdersIds = new List<int> { }
            },
            new Product { Id = 7, Name = "Bath Contraption", Category = "Beauty", Price = 106.99M, SellPrice = 84.95M , ListDate = DateTime.Now,
                Orders = new List<ProductOrder>
                {
                    new ProductOrder { Id = 1, ClientName = "Michael" }
                },
                OrdersIds = new List<int> { 1 }
            },
            new Product { Id = 8, Name = "*Example \\With*", Category = "Activity", Price = 450.95M, SellPrice = 76 , ListDate = DateTime.Now.AddDays(-7),
                Orders = new List<ProductOrder>{ },
                OrdersIds = new List<int> { }
            },
        };

        foreach (var item in _data)
        {
            item.Reference = item;
            item.Collection = new List<Product> { item, item };
            item.Tags = new List<Tag> 
            {
                new Tag { Value = $"Tag{item.Id}" }
            };
        }
    }

    public static IQueryable<Product> Query() => _data.Select(s => s).AsQueryable();
}