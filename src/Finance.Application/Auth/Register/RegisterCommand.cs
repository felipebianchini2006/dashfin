using Finance.Application.Auth.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Auth.Register;

public sealed record RegisterCommand(string Email, string Password) : IRequest<Result<UserProfileDto>>;

