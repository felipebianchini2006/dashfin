using Finance.Domain.Common;

namespace Finance.Domain.Entities;

public sealed class Budget : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public Guid CategoryId { get; set; }
  public DateOnly Month { get; set; }
  public decimal LimitAmount { get; set; }
}
