using System.Text;
using Finance.Application.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Finance.Api.Health;

public sealed class StorageHealthCheck : IHealthCheck
{
  private readonly IFileStorage _storage;

  public StorageHealthCheck(IFileStorage storage)
  {
    _storage = storage;
  }

  public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    var key = $"health/{Guid.NewGuid():N}.txt";
    try
    {
      await using (var content = new MemoryStream(Encoding.UTF8.GetBytes("ok")))
      {
        await _storage.SaveAsync(key, content, "text/plain", cancellationToken);
      }

      await using (var read = await _storage.OpenReadAsync(key, cancellationToken))
      {
        using var ms = new MemoryStream();
        await read.CopyToAsync(ms, cancellationToken);
        if (ms.Length == 0)
          return HealthCheckResult.Unhealthy("Storage read returned empty content.");
      }

      await _storage.DeleteAsync(key, cancellationToken);
      return HealthCheckResult.Healthy($"Storage ok (provider={_storage.Provider}).");
    }
    catch (Exception ex)
    {
      try { await _storage.DeleteAsync(key, cancellationToken); } catch { }
      return HealthCheckResult.Unhealthy($"Storage check failed (provider={_storage.Provider}).", ex);
    }
  }
}

