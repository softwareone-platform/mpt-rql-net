using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rql.Sample.Application.Products.Queries.ListAddresses;
using Rql.Sample.Application.Products.Queries.ListProducts;
using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using SoftwareOne.Rql;

namespace Rql.Sample.Api.Controllers
{

    [ApiController]
    [Route("ef")]
    public class EfController : ControllerBase
    {
        private readonly ILogger<InMemoryController> _logger;
        private readonly ISender _mediator;

        public EfController(ILogger<InMemoryController> logger, ISender mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("addresses")]
        [Produces(typeof(IEnumerable<Address>))]
        public async Task<IActionResult> Addresses(
            [FromServices] IRqlRequest<Address> rql)
        {
            var data = await _mediator.Send(new ListAddressesQuery());
            return await rql.ProcessAsync(data);
        }

        [HttpGet("products")]
        [Produces(typeof(IEnumerable<Product>))]
        public async Task<IActionResult> Products(
            [FromServices] IRqlRequest<Product> rql)
        {
            var data = await _mediator.Send(new ListProductsQuery());
            return await rql.ProcessAsync(data);
        }

        [HttpGet("products/view")]
        [Produces(typeof(IEnumerable<ProductView>))]
        public async Task<IActionResult> ProductsView(
            [FromServices] IRqlRequest<Product, ProductView> rql)
        {
            var data = await _mediator.Send(new ListProductsQuery());
            return await rql.ProcessAsync(data);
        }
    }
}