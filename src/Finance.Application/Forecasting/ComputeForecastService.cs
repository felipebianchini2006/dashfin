using Finance.Application.Abstractions;
using Finance.Application.Forecasting.Models;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Forecasting;

public sealed class ComputeForecastService
{
  private readonly IAppDbContext _db;
  private readonly IClock _clock;

  public ComputeForecastService(IAppDbContext db, IClock clock)
  {
    _db = db;
    _clock = clock;
  }

  public async Task<ForecastDto> ComputeAsync(Guid userId, DateOnly month, CancellationToken ct)
  {
    var normalizedMonth = new DateOnly(month.Year, month.Month, 1);
    var monthStart = new DateTimeOffset(normalizedMonth.Year, normalizedMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
    var monthEnd = monthStart.AddMonths(1);

    var now = _clock.UtcNow;

    // If the requested month is not current, compute "as of" at the end of that month (past) or the start (future).
    var asOf = now < monthStart
      ? monthStart
      : now >= monthEnd ? monthEnd : now;

    var asOfDate = DateOnly.FromDateTime(asOf.UtcDateTime);
    if (asOf >= monthEnd)
      asOfDate = new DateOnly(normalizedMonth.Year, normalizedMonth.Month, DateTime.DaysInMonth(normalizedMonth.Year, normalizedMonth.Month));
    if (asOf <= monthStart)
      asOfDate = normalizedMonth;

    var asOfExclusive = asOf >= monthEnd ? monthEnd : asOf;

    var daysInMonth = DateTime.DaysInMonth(normalizedMonth.Year, normalizedMonth.Month);
    var elapsedDays = asOfExclusive <= monthStart ? 0 : Math.Clamp(asOfDate.Day, 1, daysInMonth);
    var remainingDays = Math.Max(0, daysInMonth - elapsedDays);

    var txQuery = _db.Transactions
      .AsNoTracking()
      .Where(t =>
        t.UserId == userId &&
        t.OccurredAt >= monthStart &&
        t.OccurredAt < asOfExclusive &&
        t.Amount < 0m &&
        !t.IgnoreInDashboard);

    // Daily spend per category (month-to-date).
    var totalDaily = await txQuery
      .GroupBy(t => t.OccurredAt.Date)
      .Select(g => new
      {
        Day = g.Key,
        Spent = -g.Sum(t => t.Amount)
      })
      .ToListAsync(ct);

    var daily = await txQuery
      .Where(t => t.CategoryId != null)
      .GroupBy(t => new { CategoryId = t.CategoryId!.Value, Day = t.OccurredAt.Date })
      .Select(g => new
      {
        g.Key.CategoryId,
        Day = g.Key.Day,
        Spent = -g.Sum(t => t.Amount)
      })
      .ToListAsync(ct);

    var totalSpentToDate = await txQuery.Select(t => (decimal?)t.Amount).SumAsync(ct) ?? 0m;
    totalSpentToDate = -totalSpentToDate;

    var categories = await _db.Categories
      .AsNoTracking()
      .Where(c => c.UserId == userId)
      .Select(c => new { c.Id, c.Name })
      .ToListAsync(ct);

    var categoryName = categories.ToDictionary(c => c.Id, c => c.Name);

    var budgets = await _db.Budgets
      .AsNoTracking()
      .Where(b => b.UserId == userId && b.Month == normalizedMonth)
      .Select(b => new { b.CategoryId, b.LimitAmount })
      .ToListAsync(ct);

    var budgetLimit = budgets.ToDictionary(b => b.CategoryId, b => (decimal?)b.LimitAmount);

    var spentToDateByCategory = daily
      .GroupBy(x => x.CategoryId)
      .ToDictionary(g => g.Key, g => g.Sum(x => x.Spent));

    decimal GetDailyAverage(IReadOnlyList<(DateOnly Day, decimal Spent)> byDay, int windowDays)
    {
      if (elapsedDays <= 0)
        return 0m;

      var windowStart = asOfDate.AddDays(-(windowDays - 1));
      if (windowStart < normalizedMonth)
        windowStart = normalizedMonth;

      var windowLength = (asOfDate.DayNumber - windowStart.DayNumber) + 1;
      windowLength = Math.Max(1, windowLength);

      var inWindow = byDay.Where(x => x.Day >= windowStart && x.Day <= asOfDate).ToList();
      var daysWithTx = inWindow.Select(x => x.Day).Distinct().Count();

      if (daysWithTx < 3)
      {
        var monthSpent = byDay.Sum(x => x.Spent);
        return monthSpent / elapsedDays;
      }

      var windowSpent = inWindow.Sum(x => x.Spent);
      return windowSpent / windowLength;
    }

    var totalByDay = totalDaily
      .Select(x => (Day: DateOnly.FromDateTime(x.Day), x.Spent))
      .OrderBy(x => x.Day)
      .ToList();

    var avg7 = GetDailyAverage(totalByDay, 7);
    var avg14 = GetDailyAverage(totalByDay, 14);
    var usedAvg = avg7 >= avg14 ? avg7 : avg14;
    var totalProjected = totalSpentToDate + usedAvg * remainingDays;

    var categoryIds = daily.Select(x => x.CategoryId)
      .Concat(budgets.Select(b => b.CategoryId))
      .Distinct()
      .ToList();

    var dailyByCategory = daily
      .GroupBy(x => x.CategoryId)
      .ToDictionary(
        g => g.Key,
        g => g.Select(x => (Day: DateOnly.FromDateTime(x.Day), x.Spent)).OrderBy(x => x.Day).ToList());

    var perCategory = categoryIds
      .Select(categoryId =>
      {
        var byDay = dailyByCategory.GetValueOrDefault(categoryId) ?? [];
        var spent = spentToDateByCategory.GetValueOrDefault(categoryId, 0m);

        var cAvg7 = GetDailyAverage(byDay, 7);
        var cAvg14 = GetDailyAverage(byDay, 14);
        var cUsed = cAvg7 >= cAvg14 ? cAvg7 : cAvg14;
        var projected = spent + cUsed * remainingDays;

        var limit = budgetLimit.GetValueOrDefault(categoryId);
        var risk = limit is not null && projected > limit.Value;

        return new CategoryForecastDto(
          categoryId,
          categoryName.GetValueOrDefault(categoryId, "Categoria"),
          spent,
          projected,
          limit,
          risk);
      })
      .OrderByDescending(x => x.ProjectedTotal)
      .ToList();

    return new ForecastDto(normalizedMonth, asOfDate, totalSpentToDate, totalProjected, perCategory);
  }
}
