using Microsoft.EntityFrameworkCore;
using Rql.Sample.Application.Common.Interfaces.Persistence.AdventureWorks;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Infrastructure.Persistence.Ef.Repositories;

internal class ProductsRepository : IProductsRepository
{
    private readonly AvDbContext _dbContext;
    public ProductsRepository(AvDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<Product> Query() => _dbContext.Products.AsNoTracking().AsQueryable();
}
