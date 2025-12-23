using Finance.Domain.Common;

namespace Finance.Domain.Entities;

public sealed class Category : BaseEntity<Guid>, IUserOwnedEntity
{
  public Guid UserId { get; set; }
  public string Name { get; set; } = string.Empty;
  public Guid? ParentId { get; set; }
  public string? Color { get; set; }
}
