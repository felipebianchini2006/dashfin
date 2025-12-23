using Finance.Application.Abstractions;

namespace Finance.Infrastructure.Jobs;

public sealed class NoopImportJobQueue : IImportJobQueue
{
  public void EnqueueProcessImport(Guid importId) { }
}

