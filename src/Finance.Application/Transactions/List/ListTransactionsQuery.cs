using Finance.Application.Common;
using Finance.Application.Transactions.Models;
using MediatR;

namespace Finance.Application.Transactions.List;

public sealed record ListTransactionsQuery(
  DateTimeOffset? From,
  DateTimeOffset? To,
  Guid? AccountId,
  Guid? CategoryId,
  TransactionFlow? Type,
  decimal? MinAmount,
  decimal? MaxAmount,
  string? Search,
  int Page = 1,
  int PageSize = 50) : IRequest<Result<ListTransactionsResponse>>;

