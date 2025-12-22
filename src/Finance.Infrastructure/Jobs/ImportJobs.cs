using Microsoft.Extensions.Logging;

namespace Finance.Infrastructure.Jobs;

public sealed class ImportJobs
{
  private readonly ILogger<ImportJobs> _logger;
  public ImportJobs(ILogger<ImportJobs> logger) => _logger = logger;

  public Task ProcessImport(Guid importId)
  {
    _logger.LogInformation("Processing import {ImportId}", importId);
    // TODO: implement actual PDF parsing and row creation.
    return Task.CompletedTask;
  }
}

