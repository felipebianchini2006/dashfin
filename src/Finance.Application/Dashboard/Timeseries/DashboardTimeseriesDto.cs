namespace Finance.Application.Dashboard.Timeseries;

public sealed record DailySpendPointDto(DateOnly Date, decimal SpentAmount);

public sealed record DashboardTimeseriesDto(
  DateOnly Month,
  IReadOnlyList<DailySpendPointDto> Items);

