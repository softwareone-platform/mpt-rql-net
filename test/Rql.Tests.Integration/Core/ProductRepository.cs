namespace Rql.Tests.Integration.Core;

public static class ProductRepository
{
    private static readonly List<Product> _data;

    static ProductRepository()
    {
        _data = new List<Product>
        {
            new Product { Id = 1, Name = "Jewelry Widget", Category = "Clothing", Price = 192.95M, SellPrice = 172.99M, ListDate = DateTime.Now },
            new Product { Id = 2, Name = "Camping Whatchamacallit", Category = "Activity", Price = 95, SellPrice = 74.99M , ListDate = DateTime.Now },
            new Product { Id = 3, Name = "Sports Contraption", Category = "Activity", Price = 820.95M, SellPrice = 64 , ListDate = DateTime.Now.AddDays(-7) },
            new Product { Id = 4, Name = "Furniture Apparatus", Category = "Home", Price = 146, SellPrice = 50 , ListDate = DateTime.Now },
            new Product { Id = 5, Name = "Dog Whatchamacallit", Category = "Pets", Price = 205.15M, SellPrice = 3 , ListDate = DateTime.Now.AddDays(-7) },
            new Product { Id = 6, Name = "Makeup Contraption", Category = "Beauty", Price = 129.99M, SellPrice = 129.99M , ListDate = DateTime.Now.AddDays(-7) },
            new Product { Id = 7, Name = "Bath Contraption", Category = "Beauty", Price = 106.99M, SellPrice = 84.95M , ListDate = DateTime.Now },
        };

        foreach (var item in _data)
        {
            item.Reference = item;
            item.Collection = new List<Product> { item, item };
        }
    }

    public static IQueryable<Product> Query() => _data.Select(s => s).AsQueryable();
}