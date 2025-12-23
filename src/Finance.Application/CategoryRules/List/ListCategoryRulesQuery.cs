using Finance.Application.CategoryRules.Models;
using Finance.Application.Common;
using MediatR;

namespace Finance.Application.CategoryRules.List;

public sealed record ListCategoryRulesQuery() : IRequest<Result<IReadOnlyList<CategoryRuleDto>>>;

