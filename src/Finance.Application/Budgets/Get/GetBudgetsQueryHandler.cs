using Finance.Application.Abstractions;
using Finance.Application.Budgets.Models;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Budgets.Get;

internal sealed class GetBudgetsQueryHandler : IRequestHandler<GetBudgetsQuery, Result<IReadOnlyList<BudgetDto>>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public GetBudgetsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<IReadOnlyList<BudgetDto>>> Handle(GetBudgetsQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<IReadOnlyList<BudgetDto>>(Error.Unauthorized());

    var month = NormalizeMonth(request.Month);
    var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);

    var spendByCategory = _db.Transactions
      .AsNoTracking()
      .Where(t =>
        t.UserId == userId.Value &&
        t.CategoryId != null &&
        t.OccurredAt >= monthStart &&
        t.OccurredAt < monthEnd &&
        t.Amount < 0m &&
        !t.IgnoreInDashboard)
      .GroupBy(t => t.CategoryId!.Value)
      .Select(g => new
      {
        CategoryId = g.Key,
        Spent = -g.Sum(t => t.Amount)
      });

    var budgets = await (
        from b in _db.Budgets.AsNoTracking()
        where b.UserId == userId.Value && b.Month == month
        join s in spendByCategory on b.CategoryId equals s.CategoryId into sj
        from s in sj.DefaultIfEmpty()
        orderby b.CreatedAt descending
        select new BudgetDto(
          b.Id,
          b.CategoryId,
          b.Month,
          b.LimitAmount,
          s == null ? 0m : s.Spent
        ))
      .ToListAsync(ct);

    return Result.Ok<IReadOnlyList<BudgetDto>>(budgets);
  }

  private static DateOnly NormalizeMonth(DateOnly month) => new(month.Year, month.Month, 1);
}

