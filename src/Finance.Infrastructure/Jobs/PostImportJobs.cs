using Microsoft.Extensions.Logging;

namespace Finance.Infrastructure.Jobs;

public sealed class PostImportJobs
{
  private readonly ILogger<PostImportJobs> _logger;
  public PostImportJobs(ILogger<PostImportJobs> logger) => _logger = logger;

  public Task GenerateAlerts(Guid userId, int year, int month)
  {
    _logger.LogInformation("GenerateAlerts triggered (user={UserId}, year={Year}, month={Month})", userId, year, month);
    return Task.CompletedTask;
  }

  public Task ComputeForecast(Guid userId, int year, int month)
  {
    _logger.LogInformation("ComputeForecast triggered (user={UserId}, year={Year}, month={Month})", userId, year, month);
    return Task.CompletedTask;
  }
}

