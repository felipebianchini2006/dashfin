using System.Security.Cryptography;
using System.Text;
using Finance.Application.Abstractions;

namespace Finance.Application.Tests;

internal sealed class TestTokenService : ITokenService
{
  private int _refreshCounter;

  public string CreateAccessToken(Guid userId, string email) => $"access:{userId}:{email}";

  public string CreateRefreshToken()
  {
    _refreshCounter++;
    return $"refresh-{_refreshCounter}";
  }

  public string HashRefreshToken(string refreshToken)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}

