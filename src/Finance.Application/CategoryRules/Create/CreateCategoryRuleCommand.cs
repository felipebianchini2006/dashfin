using Finance.Application.CategoryRules.Models;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;

namespace Finance.Application.CategoryRules.Create;

public sealed record CreateCategoryRuleCommand(
  string Pattern,
  CategoryRuleMatchType MatchType,
  Guid CategoryId,
  int Priority,
  bool IsActive) : IRequest<Result<CategoryRuleDto>>;

