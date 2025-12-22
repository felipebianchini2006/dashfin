using Finance.Application.Abstractions;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Finance.Infrastructure.Jobs;

public sealed class HangfirePostImportTasks : IPostImportTasks
{
  private readonly IBackgroundJobClient _jobs;
  private readonly ILogger<HangfirePostImportTasks> _logger;

  public HangfirePostImportTasks(IBackgroundJobClient jobs, ILogger<HangfirePostImportTasks> logger)
  {
    _jobs = jobs;
    _logger = logger;
  }

  public void EnqueueGenerateAlerts(Guid userId, int year, int month)
  {
    _jobs.Enqueue<PostImportJobs>(j => j.GenerateAlerts(userId, year, month));
    _logger.LogInformation("Enqueued GenerateAlerts (user={UserId}, year={Year}, month={Month})", userId, year, month);
  }

  public void EnqueueComputeForecast(Guid userId, int year, int month)
  {
    _jobs.Enqueue<PostImportJobs>(j => j.ComputeForecast(userId, year, month));
    _logger.LogInformation("Enqueued ComputeForecast (user={UserId}, year={Year}, month={Month})", userId, year, month);
  }
}

