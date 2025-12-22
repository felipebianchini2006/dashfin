using Finance.Application.Abstractions;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Finance.Infrastructure.Jobs;

public sealed class HangfireImportJobQueue : IImportJobQueue
{
  private readonly IBackgroundJobClient _jobs;
  private readonly ILogger<HangfireImportJobQueue> _logger;

  public HangfireImportJobQueue(IBackgroundJobClient jobs, ILogger<HangfireImportJobQueue> logger)
  {
    _jobs = jobs;
    _logger = logger;
  }

  public void EnqueueProcessImport(Guid importId)
  {
    _jobs.Enqueue<ImportJobs>(j => j.ProcessImport(importId));
    _logger.LogInformation("Enqueued ProcessImport for {ImportId}", importId);
  }
}

