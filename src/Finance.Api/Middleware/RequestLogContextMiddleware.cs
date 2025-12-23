using Finance.Application.Abstractions;
using Serilog.Context;

namespace Finance.Api.Middleware;

public sealed class RequestLogContextMiddleware : IMiddleware
{
  private readonly ICurrentUser _currentUser;

  public RequestLogContextMiddleware(ICurrentUser currentUser)
  {
    _currentUser = currentUser;
  }

  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    var userId = _currentUser.UserId;
    var requestId = context.TraceIdentifier;
    var importId = ExtractImportId(context.Request.Path);

    using (LogContext.PushProperty("RequestId", requestId))
    using (LogContext.PushProperty("UserId", userId))
    using (LogContext.PushProperty("ImportId", importId))
    {
      await next(context);
    }
  }

  public static Guid? ExtractImportId(PathString path)
  {
    var value = path.Value ?? string.Empty;
    if (value.Length == 0)
      return null;

    var parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length >= 2 && parts[0].Equals("imports", StringComparison.OrdinalIgnoreCase))
      return Guid.TryParse(parts[1], out var id) ? id : null;

    return null;
  }
}
