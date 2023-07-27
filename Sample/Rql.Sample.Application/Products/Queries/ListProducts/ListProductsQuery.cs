using MediatR;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Application.Products.Queries.ListProducts
{
    public record ListProductsQuery() : IRequest<IQueryable<Product>>;
}
