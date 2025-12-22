namespace Finance.Infrastructure.Files;

public sealed class S3FileStorageOptions
{
  public const string SectionName = "FileStorage:S3";

  public string ServiceUrl { get; set; } = string.Empty; // e.g. https://s3.amazonaws.com or https://minio:9000
  public string Region { get; set; } = "us-east-1";
  public bool ForcePathStyle { get; set; } = true;
  public string Bucket { get; set; } = string.Empty;
  public string Prefix { get; set; } = string.Empty;
  public string AccessKey { get; set; } = string.Empty;
  public string SecretKey { get; set; } = string.Empty;
}

