using Finance.Domain.Enums;

namespace Finance.Application.Alerts.Models;

public sealed record AlertEventDto(
  Guid Id,
  Guid AlertRuleId,
  string Fingerprint,
  AlertEventStatus Status,
  DateTimeOffset OccurredAt,
  string Title,
  string? Body,
  string? PayloadJson);

