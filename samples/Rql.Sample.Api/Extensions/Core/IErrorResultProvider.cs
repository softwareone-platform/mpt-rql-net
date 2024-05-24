using Microsoft.AspNetCore.Mvc;
using SoftwareOne.Rql.Linq.Core.Result;

namespace Rql.Sample.Api.Extensions.Core;

internal interface IErrorResultProvider
{
    IActionResult Problem(List<Error> errors);
}
