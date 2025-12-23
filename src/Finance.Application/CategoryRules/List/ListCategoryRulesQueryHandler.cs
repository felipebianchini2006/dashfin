using Finance.Application.Abstractions;
using Finance.Application.CategoryRules.Models;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.CategoryRules.List;

internal sealed class ListCategoryRulesQueryHandler : IRequestHandler<ListCategoryRulesQuery, Result<IReadOnlyList<CategoryRuleDto>>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public ListCategoryRulesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<IReadOnlyList<CategoryRuleDto>>> Handle(ListCategoryRulesQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<IReadOnlyList<CategoryRuleDto>>(Error.Unauthorized());

    var rules = await _db.CategoryRules
      .AsNoTracking()
      .Where(r => r.UserId == userId.Value)
      .OrderBy(r => r.Priority)
      .ThenByDescending(r => r.CreatedAt)
      .Select(r => new CategoryRuleDto(
        r.Id,
        r.CategoryId,
        r.MatchType,
        r.Pattern,
        r.Priority,
        r.IsActive,
        r.AccountId,
        r.MinAmount,
        r.MaxAmount,
        r.CreatedAt))
      .ToListAsync(ct);

    return Result.Ok<IReadOnlyList<CategoryRuleDto>>(rules);
  }
}

