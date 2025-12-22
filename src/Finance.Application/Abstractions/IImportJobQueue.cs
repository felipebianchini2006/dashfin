namespace Finance.Application.Abstractions;

public interface IImportJobQueue
{
  void EnqueueProcessImport(Guid importId);
}

