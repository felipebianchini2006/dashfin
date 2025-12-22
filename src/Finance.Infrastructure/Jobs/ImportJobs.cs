using Finance.Application.Imports.Processing;
using Microsoft.Extensions.Logging;

namespace Finance.Infrastructure.Jobs;

public sealed class ImportJobs
{
  private readonly ILogger<ImportJobs> _logger;
  private readonly ImportProcessor _processor;

  public ImportJobs(ILogger<ImportJobs> logger, ImportProcessor processor)
  {
    _logger = logger;
    _processor = processor;
  }

  public Task ProcessImport(Guid importId)
  {
    using var scope = _logger.BeginScope(new Dictionary<string, object> { ["importId"] = importId });
    _logger.LogInformation("Hangfire job ProcessImport started");
    return RunAsync(importId);
  }

  private async Task RunAsync(Guid importId)
  {
    var result = await _processor.ProcessImportAsync(importId, CancellationToken.None);
    if (result.IsFailure)
      throw new InvalidOperationException(result.Error?.Message ?? "Import processing failed.");
  }
}
