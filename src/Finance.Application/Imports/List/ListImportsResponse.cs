using Finance.Application.Imports.Models;

namespace Finance.Application.Imports.List;

public sealed record ImportListItemDto(
  Guid Id,
  Finance.Domain.Enums.ImportStatus Status,
  string? SummaryJson,
  string? ErrorMessage,
  DateTimeOffset CreatedAt,
  ImportAccountInfoDto? Account);

public sealed record ListImportsResponse(
  IReadOnlyList<ImportListItemDto> Items,
  int Page,
  int PageSize,
  int TotalCount);

