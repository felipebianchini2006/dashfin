using Finance.Domain.Enums;

namespace Finance.Application.Imports.Models;

public sealed record ImportRowAuditDto(
  long Id,
  int RowIndex,
  int? PageNumber,
  ImportRowStatus Status,
  string? ErrorCode,
  string? ErrorMessage,
  DateTimeOffset CreatedAt);

