using Finance.Application.Transactions.Models;

namespace Finance.Application.Transactions.List;

public sealed record ListTransactionsResponse(
  IReadOnlyList<TransactionDto> Items,
  int Page,
  int PageSize,
  int TotalCount);

