namespace Finance.Application.Dashboard.Balances;

public sealed record AccountBalanceDto(
  Guid AccountId,
  string Name,
  string Currency,
  decimal Balance);

public sealed record DashboardBalancesDto(
  IReadOnlyList<AccountBalanceDto> CheckingAccounts,
  decimal TotalSaved,
  decimal? CreditCardOpen,
  decimal? NetWorth);

