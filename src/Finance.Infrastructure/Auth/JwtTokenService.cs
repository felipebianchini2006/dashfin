using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Finance.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Finance.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
  private readonly JwtOptions _options;
  public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

  public string CreateAccessToken(Guid userId, string email)
  {
    var claims = new[]
    {
      new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
      new Claim(JwtRegisteredClaimNames.Email, email),
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
      issuer: _options.Issuer,
      audience: _options.Audience,
      claims: claims,
      expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
      signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }

  public string CreateRefreshToken()
  {
    var bytes = RandomNumberGenerator.GetBytes(32);
    return Convert.ToBase64String(bytes);
  }

  public string HashRefreshToken(string refreshToken)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
    return Convert.ToHexString(bytes).ToLowerInvariant();
  }
}

