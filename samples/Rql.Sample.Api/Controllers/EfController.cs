using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rql.Sample.Application.Products.Queries.ListAddresses;
using Rql.Sample.Application.Products.Queries.ListProducts;
using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using Mpt.Rql;

namespace Rql.Sample.Api.Controllers;

[ApiController]
[Route("ef")]
public class EfController : ControllerBase
{
    private readonly ISender _mediator;

    public EfController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("addresses")]
    [Produces(typeof(ListResponse<Address>))]
    public async Task<IActionResult> Addresses(
        [FromServices] IRqlRequest<Address> rql)
    {
        var data = await _mediator.Send(new ListAddressesQuery(string.Empty));
        return await rql.ProcessAsync(data);
    }

    [HttpGet("products")]
    [Produces(typeof(ListResponse<Product>))]
    public async Task<IActionResult> Products(
        [FromServices] IRqlRequest<Product> rql)
    {
        var data = await _mediator.Send(new ListProductsQuery(string.Empty));
        return await rql.ProcessAsync(data);
    }

    [HttpGet("products/view")]
    [Produces(typeof(ListResponse<ProductView>))]
    public async Task<IActionResult> ProductsView(
        [FromServices] IRqlRequest<Product, ProductView> rql)
    {
        var data = await _mediator.Send(new ListProductsQuery(string.Empty));
        return await rql.ProcessAsync(data);
    }
}
