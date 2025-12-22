using Finance.Domain.Common;

namespace Finance.Domain.Entities;

public sealed class UserRefreshToken : BaseEntity<Guid>
{
  public Guid UserId { get; set; }
  public string TokenHash { get; set; } = string.Empty;
  public DateTimeOffset ExpiresAt { get; set; }
  public DateTimeOffset? RevokedAt { get; set; }
  public string? ReplacedByTokenHash { get; set; }
  public string? RevokedReason { get; set; }
}

