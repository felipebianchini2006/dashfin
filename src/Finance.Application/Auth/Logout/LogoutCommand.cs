using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Auth.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;

