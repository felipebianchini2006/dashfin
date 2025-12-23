using Microsoft.Extensions.Logging;
using Finance.Application.Alerts.Generate;
using Finance.Application.Forecasting;

namespace Finance.Infrastructure.Jobs;

public sealed class PostImportJobs
{
  private readonly ILogger<PostImportJobs> _logger;
  private readonly GenerateAlertsService _alerts;
  private readonly ComputeForecastService _forecast;

  public PostImportJobs(ILogger<PostImportJobs> logger, GenerateAlertsService alerts, ComputeForecastService forecast)
  {
    _logger = logger;
    _alerts = alerts;
    _forecast = forecast;
  }

  public Task GenerateAlerts(Guid userId, int year, int month)
  {
    _logger.LogInformation("GenerateAlerts triggered (user={UserId}, year={Year}, month={Month})", userId, year, month);
    return _alerts.GenerateAsync(userId, year, month, CancellationToken.None);
  }

  public Task ComputeForecast(Guid userId, int year, int month)
  {
    _logger.LogInformation("ComputeForecast triggered (user={UserId}, year={Year}, month={Month})", userId, year, month);
    return _forecast.ComputeAsync(userId, new DateOnly(year, month, 1), CancellationToken.None);
  }
}
