using Finance.Domain.Common;
using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class AlertEvent : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public Guid AlertRuleId { get; set; }
  public AlertEventStatus Status { get; set; }
  public DateTimeOffset OccurredAt { get; set; }
  public string Title { get; set; } = string.Empty;
  public string? Body { get; set; }
  public string? PayloadJson { get; set; }
}
