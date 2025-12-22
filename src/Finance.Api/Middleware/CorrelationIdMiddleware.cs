using Serilog.Context;

namespace Finance.Api.Middleware;

public sealed class CorrelationIdMiddleware : IMiddleware
{
  public const string HeaderName = "X-Correlation-Id";

  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue)
      ? headerValue.ToString()
      : Guid.NewGuid().ToString("N");

    context.Items[HeaderName] = correlationId;
    context.Response.Headers[HeaderName] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
      await next(context);
    }
  }
}

