using Finance.Application.Abstractions;
using Finance.Application.Auth.Models;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Auth.Me;

internal sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<UserProfileDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public GetMeQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<UserProfileDto>> Handle(GetMeQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<UserProfileDto>(Error.Unauthorized());

    var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId.Value, ct);
    if (user is null)
      return Result.Fail<UserProfileDto>(Error.Unauthorized());

    return Result.Ok(new UserProfileDto(
      user.Email,
      user.Timezone,
      user.Currency,
      new UserDisplayPreferencesDto(user.DisplayPreferences.Theme, user.DisplayPreferences.CompactMode)));
  }
}

