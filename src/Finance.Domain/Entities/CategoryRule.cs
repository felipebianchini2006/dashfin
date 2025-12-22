using Finance.Domain.Common;
using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class CategoryRule : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public Guid CategoryId { get; set; }
  public CategoryRuleMatchType MatchType { get; set; }
  public string Pattern { get; set; } = string.Empty;
  public Guid? AccountId { get; set; }
  public int Priority { get; set; } = 100;
  public bool IsActive { get; set; } = true;
  public decimal? MinAmount { get; set; }
  public decimal? MaxAmount { get; set; }
}
