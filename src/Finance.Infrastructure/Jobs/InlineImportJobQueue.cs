using Finance.Application.Abstractions;
using Finance.Application.Imports.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Finance.Infrastructure.Jobs;

/// <summary>
/// Synchronous "queue" for tests/dev: processes the import immediately.
/// </summary>
public sealed class InlineImportJobQueue : IImportJobQueue
{
  private readonly IServiceScopeFactory _scopes;
  private readonly ILogger<InlineImportJobQueue> _logger;

  public InlineImportJobQueue(IServiceScopeFactory scopes, ILogger<InlineImportJobQueue> logger)
  {
    _scopes = scopes;
    _logger = logger;
  }

  public void EnqueueProcessImport(Guid importId)
  {
    try
    {
      using var scope = _scopes.CreateScope();
      var processor = scope.ServiceProvider.GetRequiredService<ImportProcessor>();
      processor.ProcessImportAsync(importId, CancellationToken.None).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Inline import processing failed for {ImportId}", importId);
      throw;
    }
  }
}

