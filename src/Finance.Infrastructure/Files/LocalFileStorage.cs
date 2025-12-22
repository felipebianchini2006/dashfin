using Finance.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.Files;

public sealed class LocalFileStorage : IFileStorage
{
  private readonly string _root;

  public LocalFileStorage(IOptions<LocalFileStorageOptions> options)
  {
    _root = options.Value.RootPath;
    Directory.CreateDirectory(_root);
  }

  public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
  {
    var path = Path.Combine(_root, key);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await using var fs = File.Create(path);
    await content.CopyToAsync(fs, ct);
  }

  public Task<Stream> OpenReadAsync(string key, CancellationToken ct)
  {
    var path = Path.Combine(_root, key);
    Stream stream = File.OpenRead(path);
    return Task.FromResult(stream);
  }

  public Task DeleteAsync(string key, CancellationToken ct)
  {
    var path = Path.Combine(_root, key);
    if (File.Exists(path))
      File.Delete(path);
    return Task.CompletedTask;
  }
}

