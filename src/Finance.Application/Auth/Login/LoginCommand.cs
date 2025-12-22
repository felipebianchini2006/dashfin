using Finance.Application.Auth.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokens>>;

