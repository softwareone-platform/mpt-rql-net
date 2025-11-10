using MediatR;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Application.Products.Queries.ListAddresses;

public record ListAddressesQuery(string AccountId) : IRequest<IQueryable<Address>>;
