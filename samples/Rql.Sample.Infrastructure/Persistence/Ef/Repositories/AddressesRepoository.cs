using Microsoft.EntityFrameworkCore;
using Rql.Sample.Application.Common.Interfaces.Persistence.AdventureWorks;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Infrastructure.Persistence.Ef.Repositories
{
    internal class AddressesRepoository : IAddressesRepository
    {
        private readonly AvDbContext _dbContext;
        public AddressesRepoository(AvDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<Address> Query() => _dbContext.Addresses.AsNoTracking().AsQueryable();
    }
}
