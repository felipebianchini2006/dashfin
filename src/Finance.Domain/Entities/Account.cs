using Finance.Domain.Common;
using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class Account : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public AccountType Type { get; set; }
  public string Name { get; set; } = string.Empty;
  public string? Institution { get; set; }
  public string Currency { get; set; } = "BRL";
  public decimal InitialBalance { get; set; }
}
