using MediatR;
using Rql.Sample.Application.Common.Interfaces.Persistence.AdventureWorks;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Application.Products.Queries.ListAddresses
{
    public class ListAddressesHandler : IRequestHandler<ListAddressesQuery, IQueryable<Address>>
    {
        private readonly IAddressesRepository _repository;
        public ListAddressesHandler(IAddressesRepository repository)
        {
            _repository = repository;
        }

        public Task<IQueryable<Address>> Handle(ListAddressesQuery request, CancellationToken cancellationToken)
            => Task.FromResult(_repository.Query());
    }
}
