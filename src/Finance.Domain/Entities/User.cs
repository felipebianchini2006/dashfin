using Finance.Domain.Common;

namespace Finance.Domain.Entities;

public sealed class User : BaseEntity<Guid>
{
  public string Email { get; set; } = string.Empty;
  public string? DisplayName { get; set; }
  public string PasswordHash { get; set; } = string.Empty;
  public string Timezone { get; set; } = "America/Sao_Paulo";
  public string Currency { get; set; } = "BRL";
  public UserDisplayPreferences DisplayPreferences { get; set; } = new();
}

public sealed class UserDisplayPreferences
{
  public string Theme { get; set; } = "system"; // system|light|dark
  public bool CompactMode { get; set; }
}
