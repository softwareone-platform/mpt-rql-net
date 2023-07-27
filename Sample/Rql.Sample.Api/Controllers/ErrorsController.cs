using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Rql.Sample.Api.Controllers
{
    [ApiController]
    public class ErrorsController : ControllerBase
    {
        [HttpGet("/error")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult Error()
        {
            Exception? exception = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

            return Problem(exception?.Message ?? "Unknown message");
        }
    }
}