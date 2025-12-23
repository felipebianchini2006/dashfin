using Finance.Application.Abstractions;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Dashboard.Categories;

internal sealed class GetDashboardCategoriesQueryHandler : IRequestHandler<GetDashboardCategoriesQuery, Result<DashboardCategoriesDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public GetDashboardCategoriesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<DashboardCategoriesDto>> Handle(GetDashboardCategoriesQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<DashboardCategoriesDto>(Error.Unauthorized());

    var month = NormalizeMonth(request.Month);
    var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);

    var items = await (
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
        select new CategorySpendDto(
          g.Key.CategoryId!.Value,
          g.Key.Name,
          -g.Sum(x => x.Amount)))
      .ToListAsync(ct);

    return Result.Ok(new DashboardCategoriesDto(month, items));
  }

  private static DateOnly NormalizeMonth(DateOnly month) => new(month.Year, month.Month, 1);
}

