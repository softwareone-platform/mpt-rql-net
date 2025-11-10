using Rql.Sample.Application.Common.Interfaces.Persistence.InMemory;
using Rql.Sample.Domain.InMemory;

namespace Rql.Sample.Infrastructure.Persistence.InMemory.Repositories;

public class SampleRepository : ISampleRepository
{
    private readonly List<SampleEntity> _data;
    public SampleRepository()
    {
        _data = new List<SampleEntity>
        {
            new SampleEntity { Id = 1, ProductName = "Jewelry Widget", Category = "Clothing", Price = 192.95M,  SalePrice = 172.99M, ListDate = DateTime.Now },
            new SampleEntity { Id = 2, ProductName = "Camping Whatchamacallit", Category = "Activity", Price = 95, SalePrice = 74.99M , ListDate = DateTime.Now },
            new SampleEntity { Id = 3, ProductName = "Sports Contraption", Category = "Activity", Price = 820.95M, SalePrice = 64 , ListDate = DateTime.Now.AddDays(-7) },
            new SampleEntity { Id = 4, ProductName = "Furniture Apparatus", Category = "Home", Price = 146, SalePrice = 50 , ListDate = DateTime.Now },
            new SampleEntity { Id = 5, ProductName = "Dog Whatchamacallit", Category = "Pets", Price = 205.15M, SalePrice = 3 , ListDate = DateTime.Now.AddDays(-7) },
            new SampleEntity { Id = 6, ProductName = "Makeup Contraption", Category = "Beauty", Price = 129.99M, SalePrice = 129.99M , ListDate = DateTime.Now.AddDays(-7) },
            new SampleEntity { Id = 7, ProductName = "Bath Contraption", Category = "Beauty", Price = 106.99M, SalePrice = 84.95M , ListDate = DateTime.Now },
        };
    }

    public IQueryable<SampleEntity> Query() => _data.AsQueryable();
}
