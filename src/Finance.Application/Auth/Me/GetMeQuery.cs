using Finance.Application.Auth.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Auth.Me;

public sealed record GetMeQuery : IRequest<Result<UserProfileDto>>;

