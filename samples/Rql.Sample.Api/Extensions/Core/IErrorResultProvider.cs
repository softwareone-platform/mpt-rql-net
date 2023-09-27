using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace Rql.Sample.Api.Extensions.Core;

internal interface IErrorResultProvider
{
    IActionResult Problem(List<Error> errors);
}
