namespace Finance.Application.Budgets.Models;

public sealed record BudgetDto(
  Guid Id,
  Guid CategoryId,
  DateOnly Month,
  decimal LimitAmount,
  decimal SpentAmount);

