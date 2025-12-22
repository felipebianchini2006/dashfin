using Finance.Application.Abstractions;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Auth.Logout;

internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
  private readonly IAppDbContext _db;
  private readonly ITokenService _tokens;
  private readonly IClock _clock;

  public LogoutCommandHandler(IAppDbContext db, ITokenService tokens, IClock clock)
  {
    _db = db;
    _tokens = tokens;
    _clock = clock;
  }

  public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
  {
    var hash = _tokens.HashRefreshToken(request.RefreshToken);
    var token = await _db.UserRefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
    if (token is null)
      return Result.Ok(); // idempotent

    if (token.RevokedAt is null)
    {
      token.RevokedAt = _clock.UtcNow;
      token.RevokedReason = "logout";
      await _db.SaveChangesAsync(ct);
    }

    return Result.Ok();
  }
}

