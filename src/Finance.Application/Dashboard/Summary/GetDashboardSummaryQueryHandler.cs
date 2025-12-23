using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Dashboard.Summary;

internal sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public GetDashboardSummaryQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<DashboardSummaryDto>(Error.Unauthorized());

    var month = NormalizeMonth(request.Month);
    var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);

    var baseTx = from t in _db.Transactions.AsNoTracking()
                 join a in _db.Accounts.AsNoTracking() on t.AccountId equals a.Id
                 where t.UserId == userId.Value &&
                       a.UserId == userId.Value &&
                       t.OccurredAt >= monthStart &&
                       t.OccurredAt < monthEnd &&
                       !t.IgnoreInDashboard
                 select new { t.Amount, a.Type, t.CategoryId };

    var aggregates = await baseTx
      .GroupBy(_ => 1)
      .Select(g => new
      {
        Income = g.Sum(x => x.Type != AccountType.CreditCard && x.Amount > 0m ? x.Amount : 0m),
        CheckingOut = g.Sum(x => x.Type == AccountType.Checking && x.Amount < 0m ? -x.Amount : 0m),
        CardSpend = g.Sum(x => x.Type == AccountType.CreditCard && x.Amount < 0m ? -x.Amount : 0m),
        NetCash = g.Sum(x => x.Type != AccountType.CreditCard ? x.Amount : 0m)
      })
      .SingleOrDefaultAsync(ct);

    var top = await (
        from t in _db.Transactions.AsNoTracking()
        join c in _db.Categories.AsNoTracking() on t.CategoryId equals c.Id
        where t.UserId == userId.Value &&
              c.UserId == userId.Value &&
              t.CategoryId != null &&
              t.OccurredAt >= monthStart &&
              t.OccurredAt < monthEnd &&
              t.Amount < 0m &&
              !t.IgnoreInDashboard
        group t by new { t.CategoryId, c.Name } into g
        orderby g.Sum(x => x.Amount) ascending
        select new TopCategoryDto(
          g.Key.CategoryId!.Value,
          g.Key.Name,
          -g.Sum(x => x.Amount)))
      .Take(5)
      .ToListAsync(ct);

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
      .Select(g => new { CategoryId = g.Key, Spent = -g.Sum(t => t.Amount) });

    var budgetProgress = await (
        from b in _db.Budgets.AsNoTracking()
        join c in _db.Categories.AsNoTracking() on b.CategoryId equals c.Id
        where b.UserId == userId.Value && c.UserId == userId.Value && b.Month == month
        join s in spendByCategory on b.CategoryId equals s.CategoryId into sj
        from s in sj.DefaultIfEmpty()
        orderby b.LimitAmount descending
        select new BudgetProgressDto(
          b.CategoryId,
          c.Name,
          s == null ? 0m : s.Spent,
          b.LimitAmount,
          b.LimitAmount == 0m ? 0m : ((s == null ? 0m : s.Spent) / b.LimitAmount) * 100m,
          s != null && s.Spent > b.LimitAmount))
      .ToListAsync(ct);

    var dto = new DashboardSummaryDto(
      month,
      aggregates?.Income ?? 0m,
      aggregates?.CheckingOut ?? 0m,
      aggregates?.CardSpend ?? 0m,
      aggregates?.NetCash ?? 0m,
      top,
      budgetProgress);

    return Result.Ok(dto);
  }

  private static DateOnly NormalizeMonth(DateOnly month) => new(month.Year, month.Month, 1);
}

