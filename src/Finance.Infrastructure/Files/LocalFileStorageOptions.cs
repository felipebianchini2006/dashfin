namespace Finance.Infrastructure.Files;

public sealed class LocalFileStorageOptions
{
  public const string SectionName = "FileStorage:Local";
  public string RootPath { get; set; } = "var/files";
}

