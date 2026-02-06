namespace Rql.Tests.Integration.Core;

public static class ShapedProductRepository
{
    public static IQueryable<ShapedProduct> Query()
    {
        return (new List<ShapedProduct>
        {
            new() { Id = 1, Name = "Jewelry Widget", Category = "Clothing", Price = 192.95M, ListDate = DateTime.Now,
                Collection = [
                    new() { Id = 1, Name = "Michael" },
                    new() { Id = 2, Name = "Tony" },
                    new() { Id = 3, Name = "Isabel" },
                ],
                HiddenCollection = [
                    new() { Id = 1, Name = "Michael" },
                    new() { Id = 2, Name = "Tony" },
                    new() { Id = 3, Name = "Isabel" },
                ],
                Ignored = new ShapedProductReference
                {
                    Id = 1,
                    Name = "Ignored"
                },
                Reference = new (){ Id = 1, Name = "Rajesh" }
            }
        }).AsQueryable();
    }
}