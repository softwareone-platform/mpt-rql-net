using Rql.Sample.Api.Mapping;
using Rql.Sample.Application.Common.Interfaces.Persistence.InMemory;
using Rql.Sample.Contracts.InMemory;
using Rql.Sample.Domain.InMemory;

namespace Rql.Tests.Integration.Mock;

public class MockProductRepository : ISampleRepository
{
    private static readonly List<SampleEntity> _data;

    static MockProductRepository()
    {
        _data = new List<SampleEntity>
        {
            new SampleEntity { Id = 1, ProductName = "Jewelry Widget", Category = "Clothing", Price = 192.95M, SalePrice = 172.99M, ListDate = DateTime.Now },
            new SampleEntity { Id = 2, ProductName = "Camping Whatchamacallit", Category = "Activity", Price = 95, SalePrice = 74.99M , ListDate = DateTime.Now },
            new SampleEntity { Id = 3, ProductName = "Sports Contraption", Category = "Activity", Price = 820.95M, SalePrice = 64 , ListDate = DateTime.Now.AddDays(-7) },
            new SampleEntity { Id = 4, ProductName = "Furniture Apparatus", Category = "Home", Price = 146, SalePrice = 50 , ListDate = DateTime.Now },
            new SampleEntity { Id = 5, ProductName = "Dog Whatchamacallit", Category = "Pets", Price = 205.15M, SalePrice = 3 , ListDate = DateTime.Now.AddDays(-7) },
            new SampleEntity { Id = 6, ProductName = "Makeup Contraption", Category = "Beauty", Price = 129.99M, SalePrice = 129.99M , ListDate = DateTime.Now.AddDays(-7) },
            new SampleEntity { Id = 7, ProductName = "Bath Contraption", Category = "Beauty", Price = 106.99M, SalePrice = 84.95M , ListDate = DateTime.Now },
        };

        View = Queryable.Select(_data.AsQueryable(), new SampleEntityMapper().GetMapping()).ToList();
    }

    public IQueryable<SampleEntity> Query() => _data.AsQueryable();

    public static IReadOnlyList<SampleEntityView> View { get; private set; }
}