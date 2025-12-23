namespace Finance.Application.Dashboard.Categories;

public sealed record CategorySpendDto(
  Guid CategoryId,
  string CategoryName,
  decimal SpentAmount);

public sealed record DashboardCategoriesDto(
  DateOnly Month,
  IReadOnlyList<CategorySpendDto> Items);

