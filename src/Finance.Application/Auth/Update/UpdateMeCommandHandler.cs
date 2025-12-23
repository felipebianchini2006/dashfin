using Finance.Application.Abstractions;
using Finance.Application.Auth.Models;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Auth.Update;

internal sealed class UpdateMeCommandHandler : IRequestHandler<UpdateMeCommand, Result<UserProfileDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public UpdateMeCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<UserProfileDto>> Handle(UpdateMeCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<UserProfileDto>(Error.Unauthorized());

    var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId.Value, ct);
    if (user is null)
      return Result.Fail<UserProfileDto>(Error.Unauthorized());

    if (request.Timezone is not null)
    {
      var tz = request.Timezone.Trim();
      if (tz.Length == 0)
        return Result.Fail<UserProfileDto>(Error.Validation("Timezone is required."));
      user.Timezone = tz;
    }

    if (request.Currency is not null)
    {
      var c = request.Currency.Trim().ToUpperInvariant();
      if (c.Length != 3 || c.Any(ch => ch is < 'A' or > 'Z'))
        return Result.Fail<UserProfileDto>(Error.Validation("Currency must be a 3-letter ISO code (e.g. BRL)."));
      user.Currency = c;
    }

    if (request.Theme is not null)
    {
      var theme = request.Theme.Trim();
      user.DisplayPreferences.Theme = theme;
    }

    if (request.CompactMode is not null)
      user.DisplayPreferences.CompactMode = request.CompactMode.Value;

    await _db.SaveChangesAsync(ct);

    return Result.Ok(new UserProfileDto(
      user.Email,
      user.Timezone,
      user.Currency,
      new UserDisplayPreferencesDto(user.DisplayPreferences.Theme, user.DisplayPreferences.CompactMode)));
  }
}

