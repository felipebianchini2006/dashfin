using Finance.Application.Accounts.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Accounts.Update;

public sealed record UpdateAccountCommand(Guid AccountId, string? Name, decimal? InitialBalance) : IRequest<Result<AccountDto>>;

