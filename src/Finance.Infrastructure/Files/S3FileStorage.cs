using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Finance.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Finance.Infrastructure.Files;

public sealed class S3FileStorage : IFileStorage
{
  private readonly IAmazonS3 _s3;
  private readonly S3FileStorageOptions _options;
  private readonly ILogger<S3FileStorage> _logger;

  public string Provider => "s3";

  public S3FileStorage(IOptions<S3FileStorageOptions> options, ILogger<S3FileStorage> logger)
  {
    _options = options.Value;
    _logger = logger;

    var creds = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
    var cfg = new AmazonS3Config
    {
      ServiceURL = _options.ServiceUrl,
      ForcePathStyle = _options.ForcePathStyle,
      RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
    };
    _s3 = new AmazonS3Client(creds, cfg);
  }

  public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
  {
    var fullKey = BuildKey(key);
    _logger.LogInformation("S3 upload {Bucket}/{Key}", _options.Bucket, fullKey);

    var req = new PutObjectRequest
    {
      BucketName = _options.Bucket,
      Key = fullKey,
      InputStream = content,
      ContentType = contentType
    };
    await _s3.PutObjectAsync(req, ct);
  }

  public async Task<Stream> OpenReadAsync(string key, CancellationToken ct)
  {
    var fullKey = BuildKey(key);
    var resp = await _s3.GetObjectAsync(_options.Bucket, fullKey, ct);
    return new S3ObjectResponseStream(resp);
  }

  public async Task DeleteAsync(string key, CancellationToken ct)
  {
    var fullKey = BuildKey(key);
    await _s3.DeleteObjectAsync(_options.Bucket, fullKey, ct);
  }

  private string BuildKey(string key)
  {
    var prefix = (_options.Prefix ?? string.Empty).Trim().Trim('/');
    var clean = key.TrimStart('/');
    return string.IsNullOrEmpty(prefix) ? clean : $"{prefix}/{clean}";
  }

  private sealed class S3ObjectResponseStream : Stream
  {
    private readonly GetObjectResponse _response;
    private readonly Stream _inner;

    public S3ObjectResponseStream(GetObjectResponse response)
    {
      _response = response;
      _inner = response.ResponseStream;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
        _response.Dispose();
      base.Dispose(disposing);
    }

    public override ValueTask DisposeAsync()
    {
      _response.Dispose();
      return ValueTask.CompletedTask;
    }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => _inner.CanSeek;
    public override bool CanWrite => _inner.CanWrite;
    public override long Length => _inner.Length;
    public override long Position { get => _inner.Position; set => _inner.Position = value; }
    public override void Flush() => _inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => _inner.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      => _inner.ReadAsync(buffer, offset, count, cancellationToken);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      => _inner.WriteAsync(buffer, offset, count, cancellationToken);
  }
}

