using Finance.Application.Auth.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Auth.Update;

public sealed record UpdateMeCommand(
  string? Timezone,
  string? Currency,
  string? Theme,
  bool? CompactMode) : IRequest<Result<UserProfileDto>>;

