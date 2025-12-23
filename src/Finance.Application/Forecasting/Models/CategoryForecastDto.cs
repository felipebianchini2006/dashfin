namespace Finance.Application.Forecasting.Models;

public sealed record CategoryForecastDto(
  Guid CategoryId,
  string CategoryName,
  decimal SpentToDate,
  decimal ProjectedTotal,
  decimal? BudgetLimit,
  bool RiskOfExceedingBudget);

