using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Application.Common.Interfaces.Persistence.AdventureWorks;

public interface IProductsRepository
{
    public IQueryable<Product> Query();
}
