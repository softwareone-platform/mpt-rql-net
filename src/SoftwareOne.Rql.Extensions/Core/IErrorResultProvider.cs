using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace SoftwareOne.Rql.Extensions.Core
{
    internal interface IErrorResultProvider
    {
        IActionResult Problem(List<Error> errors);
    }
}
