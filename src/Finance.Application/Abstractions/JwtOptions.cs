namespace Finance.Application.Abstractions;

public sealed class JwtOptions
{
  public const string SectionName = "Jwt";
  public string Issuer { get; set; } = "dashfin";
  public string Audience { get; set; } = "dashfin";
  public string SigningKey { get; set; } = "CHANGE_ME_DEV_ONLY";
  public int AccessTokenMinutes { get; set; } = 15;
  public int RefreshTokenDays { get; set; } = 30;
}

