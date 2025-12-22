using Finance.Domain.Enums;

namespace Finance.Application.Imports.Models;

public sealed record ImportAccountInfoDto(Guid Id, string Name, AccountType Type, string Currency);

public sealed record ImportDto(
  Guid Id,
  ImportStatus Status,
  string? SummaryJson,
  string? ErrorMessage,
  DateTimeOffset CreatedAt,
  ImportAccountInfoDto? Account);

