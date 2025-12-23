namespace Finance.Application.Dashboard.Summary;

public sealed record TopCategoryDto(
  Guid CategoryId,
  string CategoryName,
  decimal SpentAmount);

public sealed record BudgetProgressDto(
  Guid CategoryId,
  string CategoryName,
  decimal SpentAmount,
  decimal LimitAmount,
  decimal ProgressPercent,
  bool IsOverBudget);

public sealed record DashboardSummaryDto(
  DateOnly Month,
  decimal IncomeAmount,
  decimal CheckingOutAmount,
  decimal CreditCardSpendAmount,
  decimal NetCashAmount,
  IReadOnlyList<TopCategoryDto> TopCategories,
  IReadOnlyList<BudgetProgressDto> BudgetProgress);

