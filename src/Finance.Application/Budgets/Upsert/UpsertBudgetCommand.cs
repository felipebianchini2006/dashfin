using Finance.Application.Budgets.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Budgets.Upsert;

public sealed record UpsertBudgetCommand(Guid CategoryId, DateOnly Month, decimal Amount) : IRequest<Result<BudgetDto>>;

