using Rql.Sample.Application.Common.Interfaces.Persistence.InMemory;
using Rql.Sample.Domain.InMemory;

namespace Rql.Sample.Infrastructure.Persistence.InMemory.Repositories
{
    public class SampleRepository : ISampleRepository
    {
        //private readonly AvDbContext _dbContext;
        //public ProductRepository(AvDbContext dbContext)
        //{
        //    _dbContext = dbContext;
        //}

        private readonly List<SampleEntity> _data;
        public SampleRepository()
        {
            _data = new List<SampleEntity>
            {
                new SampleEntity { Id = 1, ProductName = "Book", Description = "Books", Types = new List<ProductType> { new ProductType { Id = "id", Name = "name" }, new ProductType { Id = "id2", Name = "name2" } } },
                new SampleEntity { Id = 2, ProductName = "Book", Description = "Books", Types = new List<ProductType> { new ProductType { Id = "id", Name = "name" } } },
                new SampleEntity { Id = 3, ProductName = "Tyre", Description = "Tyres" },
                new SampleEntity { Id = 4, ProductName = "Car", Description = "Cars", ProductNumber = "" , Types = new List<ProductType> { new ProductType { Id = "id", Name = "name" } }},
                new SampleEntity { Id = 5, ProductName = "Table", Description = "Tables", ProductNumber = "123" , Types = new List<ProductType> { new ProductType { Id = "id", Name = "name" } }},
            };
        }

        public IQueryable<SampleEntity> Query() => _data.AsQueryable();
    }
}
