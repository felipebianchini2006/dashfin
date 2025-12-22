using Microsoft.AspNetCore.Http;

namespace Finance.Api.Auth;

public sealed class AuthCookieOptions
{
  public const string SectionName = "AuthCookies";

  public string RefreshTokenName { get; set; } = "refresh_token";
  public string RefreshTokenPath { get; set; } = "/auth";
  public SameSiteMode RefreshTokenSameSite { get; set; } = SameSiteMode.None;
  public bool RefreshTokenSecure { get; set; } = true;
  public string? RefreshTokenDomain { get; set; }
}

