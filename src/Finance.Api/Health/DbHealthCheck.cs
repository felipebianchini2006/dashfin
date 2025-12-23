using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Finance.Api.Health;

public sealed class DbHealthCheck : IHealthCheck
{
  private readonly IServiceScopeFactory _scopes;

  public DbHealthCheck(IServiceScopeFactory scopes)
  {
    _scopes = scopes;
  }

  public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    try
    {
      using var scope = _scopes.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
      var canConnect = await db.Database.CanConnectAsync(cancellationToken);
      return canConnect
        ? HealthCheckResult.Healthy("Database reachable.")
        : HealthCheckResult.Unhealthy("Database not reachable.");
    }
    catch (Exception ex)
    {
      return HealthCheckResult.Unhealthy("Database check failed.", ex);
    }
  }
}

