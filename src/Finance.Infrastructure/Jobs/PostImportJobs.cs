using Microsoft.Extensions.Logging;
using Finance.Application.Alerts.Generate;

namespace Finance.Infrastructure.Jobs;

public sealed class PostImportJobs
{
  private readonly ILogger<PostImportJobs> _logger;
  private readonly GenerateAlertsService _alerts;

  public PostImportJobs(ILogger<PostImportJobs> logger, GenerateAlertsService alerts)
  {
    _logger = logger;
    _alerts = alerts;
  }

  public Task GenerateAlerts(Guid userId, int year, int month)
  {
    _logger.LogInformation("GenerateAlerts triggered (user={UserId}, year={Year}, month={Month})", userId, year, month);
    return _alerts.GenerateAsync(userId, year, month, CancellationToken.None);
  }

  public Task ComputeForecast(Guid userId, int year, int month)
  {
    _logger.LogInformation("ComputeForecast triggered (user={UserId}, year={Year}, month={Month})", userId, year, month);
    return Task.CompletedTask;
  }
}
