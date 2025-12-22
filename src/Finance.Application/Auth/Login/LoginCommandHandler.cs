using Finance.Application.Abstractions;
using Finance.Application.Auth.Models;
using Finance.Application.Common;
using Finance.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Finance.Application.Auth.Login;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokens>>
{
  private readonly IAppDbContext _db;
  private readonly IPasswordHasher _passwordHasher;
  private readonly ITokenService _tokens;
  private readonly IClock _clock;
  private readonly JwtOptions _jwtOptions;

  public LoginCommandHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IClock clock,
    IOptions<JwtOptions> jwtOptions)
  {
    _db = db;
    _passwordHasher = passwordHasher;
    _tokens = tokens;
    _clock = clock;
    _jwtOptions = jwtOptions.Value;
  }

  public async Task<Result<AuthTokens>> Handle(LoginCommand request, CancellationToken ct)
  {
    var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email, ct);
    if (user is null)
      return Result.Fail<AuthTokens>(Error.Unauthorized("Invalid credentials."));

    if (!_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password))
      return Result.Fail<AuthTokens>(Error.Unauthorized("Invalid credentials."));

    return Result.Ok(await IssueTokensAsync(user, ct));
  }

  private async Task<AuthTokens> IssueTokensAsync(User user, CancellationToken ct)
  {
    var accessToken = _tokens.CreateAccessToken(user.Id, user.Email);
    var refreshToken = _tokens.CreateRefreshToken();
    var refreshHash = _tokens.HashRefreshToken(refreshToken);
    var now = _clock.UtcNow;
    var expiresAt = now.AddDays(_jwtOptions.RefreshTokenDays);

    _db.UserRefreshTokens.Add(new UserRefreshToken
    {
      Id = Guid.NewGuid(),
      UserId = user.Id,
      TokenHash = refreshHash,
      ExpiresAt = expiresAt
    });

    await _db.SaveChangesAsync(ct);
    return new AuthTokens(accessToken, refreshToken, expiresAt);
  }
}
