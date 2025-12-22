namespace Finance.Application.Abstractions;

public interface IPostImportTasks
{
  void EnqueueGenerateAlerts(Guid userId, int year, int month);
  void EnqueueComputeForecast(Guid userId, int year, int month);
}

