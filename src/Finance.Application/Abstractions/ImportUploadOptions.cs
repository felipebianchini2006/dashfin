namespace Finance.Application.Abstractions;

public sealed class ImportUploadOptions
{
  public const string SectionName = "Imports";
  public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024; // 20MB
}

