using Finance.Domain.Enums;

namespace Finance.Application.Transactions.Update;

public sealed record CategoryRuleSuggestionDto(
  string Reason,
  string Pattern,
  CategoryRuleMatchType MatchType,
  Guid CategoryId,
  int Priority,
  bool IsActive);

