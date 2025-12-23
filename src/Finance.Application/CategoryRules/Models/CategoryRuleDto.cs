using Finance.Domain.Enums;

namespace Finance.Application.CategoryRules.Models;

public sealed record CategoryRuleDto(
  Guid Id,
  Guid CategoryId,
  CategoryRuleMatchType MatchType,
  string Pattern,
  int Priority,
  bool IsActive,
  Guid? AccountId,
  decimal? MinAmount,
  decimal? MaxAmount,
  DateTimeOffset CreatedAt);

