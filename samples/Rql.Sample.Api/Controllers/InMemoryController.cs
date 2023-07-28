using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rql.Sample.Api.Model;
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
        private readonly ILogger<InMemoryController> _logger;
        private readonly ISender _mediator;

        public InMemoryController(ILogger<InMemoryController> logger, ISender mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("sample")]
        [Produces(typeof(IEnumerable<SampleEntityView>))]
        public async Task<IActionResult> InMemorySample(
            [FromServices] IRqlRequest<SampleEntity, SampleEntityView> rql,
            [FromQuery] GetRequest request)
        {
            var data = await _mediator.Send(new ListSampleEntitiesQuery());
            return await rql.ProcessAsync(data);
        }
    }
}