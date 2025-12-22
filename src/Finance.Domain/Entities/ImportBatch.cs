using Finance.Domain.Common;
using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class ImportBatch : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public Guid? AccountId { get; set; }
  public ImportStatus Status { get; set; }
  public string FileName { get; set; } = string.Empty;
  public long? FileSizeBytes { get; set; }
  public string FileSha256 { get; set; } = string.Empty;
  public string StorageProvider { get; set; } = "local";
  public string StorageKey { get; set; } = string.Empty;
  public string? SummaryJson { get; set; }
  public DateTimeOffset? ProcessedAt { get; set; }
  public string? ErrorMessage { get; set; }
}
