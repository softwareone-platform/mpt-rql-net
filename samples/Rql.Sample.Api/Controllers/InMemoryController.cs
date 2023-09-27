using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rql.Sample.Application.SampleEntities.Queries.ListSampleEntities;
using Rql.Sample.Contracts.InMemory;
using Rql.Sample.Domain.InMemory;
using SoftwareOne.Rql;

namespace Rql.Sample.Api.Controllers
{

    [ApiController]
    [Route("memory")]
    public class InMemoryController : ControllerBase
    {
        private readonly ISender _mediator;

        public InMemoryController(ISender mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("sample")]
        [Produces(typeof(ListResponse<SampleEntityView>))]
        public async Task<IActionResult> InMemorySample(
            [FromServices] IRqlRequest<SampleEntity, SampleEntityView> rql)
        {
            var data = await _mediator.Send(new ListSampleEntitiesQuery(string.Empty));
            return await rql.ProcessAsync(data);
        }
    }
}