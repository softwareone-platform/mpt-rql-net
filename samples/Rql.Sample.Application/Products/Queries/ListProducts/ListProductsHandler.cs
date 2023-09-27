using MediatR;
using Rql.Sample.Application.Common.Interfaces.Persistence.AdventureWorks;
using Rql.Sample.Domain.Ef;

namespace Rql.Sample.Application.Products.Queries.ListProducts
{
    public class ListProductsHandler : IRequestHandler<ListProductsQuery, IQueryable<Product>>
    {
        private readonly IProductsRepository _productRepository;
        public ListProductsHandler(IProductsRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public Task<IQueryable<Product>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
            => Task.FromResult(_productRepository.Query());
    }
}
