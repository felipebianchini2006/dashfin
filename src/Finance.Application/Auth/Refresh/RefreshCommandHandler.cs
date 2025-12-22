using Finance.Application.Abstractions;
using Finance.Application.Auth.Models;
using Finance.Application.Common;
using Finance.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Finance.Application.Auth.Refresh;

internal sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, Result<AuthTokens>>
{
  private readonly IAppDbContext _db;
  private readonly ITokenService _tokens;
  private readonly IClock _clock;
  private readonly JwtOptions _jwtOptions;

  public RefreshCommandHandler(IAppDbContext db, ITokenService tokens, IClock clock, IOptions<JwtOptions> jwtOptions)
  {
    _db = db;
    _tokens = tokens;
    _clock = clock;
    _jwtOptions = jwtOptions.Value;
  }

  public async Task<Result<AuthTokens>> Handle(RefreshCommand request, CancellationToken ct)
  {
    var now = _clock.UtcNow;
    var hash = _tokens.HashRefreshToken(request.RefreshToken);

    var token = await _db.UserRefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
    if (token is null || token.RevokedAt is not null || token.ExpiresAt <= now)
      return Result.Fail<AuthTokens>(Error.Unauthorized("Invalid refresh token."));

    var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == token.UserId, ct);
    if (user is null)
      return Result.Fail<AuthTokens>(Error.Unauthorized("Invalid refresh token."));

    var newRefresh = _tokens.CreateRefreshToken();
    var newHash = _tokens.HashRefreshToken(newRefresh);
    var newExpires = now.AddDays(_jwtOptions.RefreshTokenDays);

    token.RevokedAt = now;
    token.ReplacedByTokenHash = newHash;
    token.RevokedReason = "rotated";

    _db.UserRefreshTokens.Add(new UserRefreshToken
    {
      Id = Guid.NewGuid(),
      UserId = user.Id,
      TokenHash = newHash,
      ExpiresAt = newExpires
    });

    await _db.SaveChangesAsync(ct);

    var accessToken = _tokens.CreateAccessToken(user.Id, user.Email);
    return Result.Ok(new AuthTokens(accessToken, newRefresh, newExpires));
  }
}
