using Finance.Domain.Enums;

namespace Finance.Application.Accounts.Models;

public sealed record AccountDto(
  Guid Id,
  string Name,
  AccountType Type,
  string Currency,
  decimal InitialBalance,
  decimal? Balance,
  decimal? CreditCardSpendThisMonth);

