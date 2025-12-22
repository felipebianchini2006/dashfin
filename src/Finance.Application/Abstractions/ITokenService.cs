namespace Finance.Application.Abstractions;

public interface ITokenService
{
  string CreateAccessToken(Guid userId, string email);
  string CreateRefreshToken();
  string HashRefreshToken(string refreshToken);
}

