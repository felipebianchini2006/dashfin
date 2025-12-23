using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Finance.Api.Health;

public static class HealthResponseWriter
{
  public static Task WriteJson(HttpContext context, HealthReport report)
  {
    context.Response.ContentType = "application/json";

    var payload = new
    {
      status = report.Status.ToString(),
      totalDurationMs = report.TotalDuration.TotalMilliseconds,
      checks = report.Entries.ToDictionary(
        e => e.Key,
        e => new
        {
          status = e.Value.Status.ToString(),
          durationMs = e.Value.Duration.TotalMilliseconds,
          description = e.Value.Description,
          error = e.Value.Exception?.Message
        })
    };

    return context.Response.WriteAsJsonAsync(payload);
  }
}

