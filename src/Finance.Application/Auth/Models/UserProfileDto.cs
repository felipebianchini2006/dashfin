namespace Finance.Application.Auth.Models;

public sealed record UserDisplayPreferencesDto(string Theme, bool CompactMode);

public sealed record UserProfileDto(
  string Email,
  string Timezone,
  string Currency,
  UserDisplayPreferencesDto DisplayPreferences);

