using Finance.Application.Abstractions;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Dashboard.Timeseries;

internal sealed class GetDashboardTimeseriesQueryHandler : IRequestHandler<GetDashboardTimeseriesQuery, Result<DashboardTimeseriesDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public GetDashboardTimeseriesQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<DashboardTimeseriesDto>> Handle(GetDashboardTimeseriesQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<DashboardTimeseriesDto>(Error.Unauthorized());

    var month = NormalizeMonth(request.Month);
    var monthStart = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);
    var daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);

    var daily = await _db.Transactions
      .AsNoTracking()
      .Where(t =>
        t.UserId == userId.Value &&
        t.OccurredAt >= monthStart &&
        t.OccurredAt < monthEnd &&
        t.Amount < 0m &&
        !t.IgnoreInDashboard)
      .GroupBy(t => t.OccurredAt.Date)
      .Select(g => new { Day = g.Key, Spent = -g.Sum(x => x.Amount) })
      .ToListAsync(ct);

    var byDay = daily.ToDictionary(x => DateOnly.FromDateTime(x.Day), x => x.Spent);
    var items = new List<DailySpendPointDto>(daysInMonth);
    for (var d = 1; d <= daysInMonth; d++)
    {
      var date = new DateOnly(month.Year, month.Month, d);
      items.Add(new DailySpendPointDto(date, byDay.GetValueOrDefault(date, 0m)));
    }

    return Result.Ok(new DashboardTimeseriesDto(month, items));
  }

  private static DateOnly NormalizeMonth(DateOnly month) => new(month.Year, month.Month, 1);
}

