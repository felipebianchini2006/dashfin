using Finance.Application.Budgets.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.Budgets.Get;

public sealed record GetBudgetsQuery(DateOnly Month) : IRequest<Result<IReadOnlyList<BudgetDto>>>;

