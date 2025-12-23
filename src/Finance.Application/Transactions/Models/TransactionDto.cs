namespace Finance.Application.Transactions.Models;

public sealed record TransactionDto(
  Guid Id,
  Guid AccountId,
  Guid? CategoryId,
  DateTimeOffset OccurredAt,
  string Description,
  string? Notes,
  decimal Amount,
  string Currency,
  TransactionFlow Type,
  bool IgnoreInDashboard);

