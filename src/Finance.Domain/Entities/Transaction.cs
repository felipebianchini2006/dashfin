using Finance.Domain.Common;

namespace Finance.Domain.Entities;

public sealed class Transaction : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public Guid AccountId { get; set; }
  public Guid? CategoryId { get; set; }
  public Guid? ImportId { get; set; }
  public long? ImportRowId { get; set; }
  public DateTimeOffset OccurredAt { get; set; }
  public string Description { get; set; } = string.Empty;
  public decimal Amount { get; set; }
  public string Currency { get; set; } = "BRL";
  public string Fingerprint { get; set; } = string.Empty;
  public string? MetadataJson { get; set; }
}
