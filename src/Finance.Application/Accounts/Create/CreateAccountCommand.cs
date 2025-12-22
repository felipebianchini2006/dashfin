using Finance.Application.Accounts.Models;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.Accounts.Create;

public sealed record CreateAccountCommand(string Name, AccountType Type, decimal? InitialBalance) : IRequest<Result<AccountDto>>;

