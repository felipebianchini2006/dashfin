namespace Finance.Application.Forecasting.Models;

public sealed record ForecastDto(
  DateOnly Month,
  DateOnly AsOfDate,
  decimal TotalSpentToDate,
  decimal TotalProjected,
  IReadOnlyList<CategoryForecastDto> Categories);

