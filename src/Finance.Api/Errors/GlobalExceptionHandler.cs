using Finance.Api.Middleware;
using Finance.Application.Common;
using Finance.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
    HttpContext httpContext,
    Exception exception,
    CancellationToken cancellationToken)
  {
    var correlationId =
      httpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var cid) ? cid : httpContext.TraceIdentifier;

    ProblemDetails problem = exception switch
    {
      AppException app => CreateProblemFromAppError(app.Error),
      ValidationException validation => new ValidationProblemDetails(
        validation.Errors
          .GroupBy(e => e.PropertyName)
          .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()))
      {
        Title = "Validation failed",
        Status = StatusCodes.Status400BadRequest
      },
      UnauthorizedAccessException => new ProblemDetails
      {
        Title = "Unauthorized",
        Status = StatusCodes.Status401Unauthorized
      },
      _ => new ProblemDetails
      {
        Title = "Unexpected error",
        Status = StatusCodes.Status500InternalServerError
      }
    };

    problem.Extensions["correlationId"] = correlationId;

    httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
    await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    return true;
  }

  private static ProblemDetails CreateProblemFromAppError(Error error)
  {
    var status = error.Code switch
    {
      "validation_error" => StatusCodes.Status400BadRequest,
      "unauthorized" => StatusCodes.Status401Unauthorized,
      "forbidden" => StatusCodes.Status403Forbidden,
      "not_found" => StatusCodes.Status404NotFound,
      "conflict" => StatusCodes.Status409Conflict,
      _ => StatusCodes.Status500InternalServerError
    };

    return new ProblemDetails
    {
      Title = error.Code,
      Detail = error.Message,
      Status = status
    };
  }
}

