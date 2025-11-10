using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Mpt.Rql.Abstractions.Result;

namespace Rql.Sample.Api.Extensions.Core;

internal class ErrorResultProvider : IErrorResultProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ErrorResultProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IActionResult Problem(List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return Problem(Error.General("A failure has occurred"));
        }

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        return Problem(errors[0]);
    }

    private IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(statusCode: statusCode, title: error.Message);
    }

    private ObjectResult Problem(string? detail = null, string? instance = null, int? statusCode = null, string? title = null, string? type = null)
    {
        var context = _httpContextAccessor.HttpContext!;
        var problemDetailsFactory = GetProblemDetailsFactory();
        ProblemDetails problemDetails;
        if (problemDetailsFactory == null)
        {
            // ProblemDetailsFactory may be null in unit testing scenarios. Improvise to make this more testable.
            problemDetails = new ProblemDetails
            {
                Detail = detail,
                Instance = instance,
                Status = statusCode ?? 500,
                Title = title,
                Type = type,
            };
        }
        else
        {
            problemDetails = problemDetailsFactory.CreateProblemDetails(
                context,
                statusCode: statusCode ?? 500,
                title: title,
                type: type,
                detail: detail,
                instance: instance);
        }

        return new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
    }

    private IActionResult ValidationProblem(List<Error> errors)
    {
        var modelStateDictionary = new ModelStateDictionary();

        foreach (var error in errors)
        {
            modelStateDictionary.AddModelError(
                error.Path ?? error.Code,
                error.Message);
        }

        return ValidationProblem(modelStateDictionary: modelStateDictionary);
    }

    private ActionResult ValidationProblem(ModelStateDictionary modelStateDictionary)
    {
        var context = _httpContextAccessor.HttpContext!;
        var problemDetailsFactory = GetProblemDetailsFactory();
        ValidationProblemDetails? validationProblem;
        if (problemDetailsFactory == null)
        {
            // ProblemDetailsFactory may be null in unit testing scenarios. Improvise to make this more testable.
            validationProblem = new ValidationProblemDetails(modelStateDictionary);
        }
        else
        {
            validationProblem = problemDetailsFactory.CreateValidationProblemDetails(
                context,
                modelStateDictionary);
        }

        if (validationProblem is { Status: 400 })
        {
            return new BadRequestObjectResult(validationProblem);
        }

        return new ObjectResult(validationProblem)
        {
            StatusCode = validationProblem?.Status
        };
    }

    private ProblemDetailsFactory GetProblemDetailsFactory()
        => _httpContextAccessor.HttpContext!.RequestServices?.GetRequiredService<ProblemDetailsFactory>()!;
}
