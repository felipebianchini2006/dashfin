using Finance.Application.Abstractions;

namespace Finance.Infrastructure.Jobs;

public sealed class NoopPostImportTasks : IPostImportTasks
{
  public void EnqueueGenerateAlerts(Guid userId, int year, int month) { }
  public void EnqueueComputeForecast(Guid userId, int year, int month) { }
}

