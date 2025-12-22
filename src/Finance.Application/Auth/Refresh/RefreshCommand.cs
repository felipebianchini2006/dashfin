using Finance.Application.Auth.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<Result<AuthTokens>>;

