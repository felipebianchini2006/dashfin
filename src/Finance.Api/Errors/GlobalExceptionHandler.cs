using Finance.Api.Middleware;
using Finance.Application.Common;
using Finance.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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

    var traceId = httpContext.TraceIdentifier;

    ProblemDetails problem = exception switch
    {
      AppException app => CreateProblemFromAppError(app.Error),
      ValidationException validation => new ValidationProblemDetails(
        validation.Errors
          .GroupBy(e => e.PropertyName)
          .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()))
      {
        Title = "Validation failed",
        Status = StatusCodes.Status400BadRequest,
        Detail = "One or more validation errors occurred."
      },
      UnauthorizedAccessException => new ProblemDetails
      {
        Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status401Unauthorized),
        Status = StatusCodes.Status401Unauthorized,
        Detail = "Unauthorized"
      },
      _ => new ProblemDetails
      {
        Title = ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError),
        Status = StatusCodes.Status500InternalServerError,
        Detail = "Unexpected error"
      }
    };

    if (exception is AppException appEx)
      problem.Extensions["code"] = appEx.Error.Code;
    if (problem is ValidationProblemDetails)
      problem.Extensions["code"] = "validation_error";

    problem.Extensions["correlationId"] = correlationId;
    problem.Extensions["traceId"] = traceId;
    problem.Extensions.TryAdd("code", MapStatusToCode(problem.Status ?? 500));

    httpContext.Response.ContentType = "application/problem+json";
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
      Title = ReasonPhrases.GetReasonPhrase(status),
      Detail = error.Message,
      Status = status
    };
  }

  private static string MapStatusToCode(int status) =>
    status switch
    {
      StatusCodes.Status400BadRequest => "bad_request",
      StatusCodes.Status401Unauthorized => "unauthorized",
      StatusCodes.Status403Forbidden => "forbidden",
      StatusCodes.Status404NotFound => "not_found",
      StatusCodes.Status409Conflict => "conflict",
      _ => "unexpected"
    };
}
