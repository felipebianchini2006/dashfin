using Finance.Application.Abstractions;

namespace Finance.Application.Tests.Fakes;

internal sealed class FakeImportJobQueue : IImportJobQueue
{
  public List<Guid> Enqueued { get; } = new();
  public void EnqueueProcessImport(Guid importId) => Enqueued.Add(importId);
}

