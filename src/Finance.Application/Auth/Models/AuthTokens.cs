namespace Finance.Application.Auth.Models;

public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

