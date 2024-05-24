using Microsoft.EntityFrameworkCore;
using Rql.Sample.Application.Common.Interfaces.Persistence.AdventureWorks;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Infrastructure.Persistence.Ef.Repositories
{
    internal class AddressesRepository : IAddressesRepository
    {
        private readonly AvDbContext _dbContext;
        public AddressesRepository(AvDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<Address> Query() => _dbContext.Addresses.AsNoTracking().AsQueryable();
    }
}
