using Finance.Domain.Common;

namespace Finance.Domain.Entities;

public sealed class User : BaseEntity<Guid>
{
  public string Email { get; set; } = string.Empty;
  public string? DisplayName { get; set; }
}
