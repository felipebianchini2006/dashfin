namespace Finance.Application.Abstractions;

public interface IFileStorage
{
  string Provider { get; }
  Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct);
  Task<Stream> OpenReadAsync(string key, CancellationToken ct);
  Task DeleteAsync(string key, CancellationToken ct);
}
