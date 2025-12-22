using Finance.Domain.Common;
using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class AlertRule : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public AlertRuleType Type { get; set; }
  public string Name { get; set; } = string.Empty;
  public bool IsActive { get; set; } = true;
  public Guid? BudgetId { get; set; }
  public Guid? CategoryId { get; set; }
  public Guid? AccountId { get; set; }
  public decimal? ThresholdAmount { get; set; }
  public decimal? ThresholdPercent { get; set; }
}
