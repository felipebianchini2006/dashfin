using Finance.Application.Accounts.Models;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.Accounts.Update;

public sealed record UpdateAccountCommand(Guid AccountId, string? Name, AccountType? Type, decimal? InitialBalance) : IRequest<Result<AccountDto>>;
