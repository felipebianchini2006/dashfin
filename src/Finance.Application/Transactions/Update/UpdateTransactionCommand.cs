using Finance.Application.Common;
using Finance.Application.Transactions.Models;
using MediatR;

namespace Finance.Application.Transactions.Update;

public sealed record UpdateTransactionCommand(
  Guid TransactionId,
  Guid? CategoryId,
  string? Notes,
  bool? IgnoreInDashboard) : IRequest<Result<TransactionDto>>;

