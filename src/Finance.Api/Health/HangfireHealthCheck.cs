using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Finance.Api.Health;

public sealed class HangfireHealthCheck : IHealthCheck
{
  private readonly IConfiguration _config;

  public HangfireHealthCheck(IConfiguration config)
  {
    _config = config;
  }

  public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    var enabled = _config.GetValue("Hangfire:Enabled", true);
    if (!enabled)
      return Task.FromResult(HealthCheckResult.Healthy("Hangfire disabled."));

    try
    {
      var storage = JobStorage.Current;
      using var connection = storage.GetConnection();
      var api = storage.GetMonitoringApi();
      _ = api.GetStatistics();
      return Task.FromResult(HealthCheckResult.Healthy("Hangfire storage reachable."));
    }
    catch (Exception ex)
    {
      return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire check failed.", ex));
    }
  }
}

