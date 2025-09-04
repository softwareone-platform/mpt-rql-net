using Microsoft.AspNetCore.Mvc;
using SoftwareOne.Rql.Abstractions.Result;

namespace Rql.Sample.Api.Extensions.Core;

internal interface IErrorResultProvider
{
    IActionResult Problem(List<Error> errors);
}
