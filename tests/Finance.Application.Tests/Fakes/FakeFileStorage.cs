using Finance.Application.Abstractions;

namespace Finance.Application.Tests.Fakes;

internal sealed class FakeFileStorage : IFileStorage
{
  private readonly Dictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

  public string Provider { get; init; } = "local";

  public Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
  {
    using var ms = new MemoryStream();
    content.CopyTo(ms);
    _objects[key] = ms.ToArray();
    return Task.CompletedTask;
  }

  public Task<Stream> OpenReadAsync(string key, CancellationToken ct)
  {
    var bytes = _objects[key];
    Stream s = new MemoryStream(bytes, writable: false);
    return Task.FromResult(s);
  }

  public Task DeleteAsync(string key, CancellationToken ct)
  {
    _objects.Remove(key);
    return Task.CompletedTask;
  }

  public bool Contains(string key) => _objects.ContainsKey(key);
}

