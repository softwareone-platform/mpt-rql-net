using MediatR;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Application.Products.Queries.ListProducts;

public record ListProductsQuery(string AccountId) : IRequest<IQueryable<Product>>;
