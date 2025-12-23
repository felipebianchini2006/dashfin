using Finance.Application.Forecasting;
using Finance.Domain.Entities;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class ForecastTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task Forecast_uses_higher_of_avg7_vs_avg14_and_flags_risk_when_projection_exceeds_budget_28_days()
  {
    await using var db = CreateDb();
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 02, 20, 12, 0, 0, TimeSpan.Zero) };
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();

    var food = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Alimentação" };
    db.Categories.Add(food);
    db.Budgets.Add(new Budget
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      CategoryId = food.Id,
      Month = new DateOnly(2025, 02, 01),
      LimitAmount = 150m
    });

    for (var day = 7; day <= 13; day++)
    {
      db.Transactions.Add(new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = food.Id,
        OccurredAt = new DateTimeOffset(2025, 02, day, 12, 0, 0, TimeSpan.Zero),
        Description = "Food",
        Amount = -5m,
        Currency = "BRL",
        Fingerprint = $"f{day}"
      });
    }

    for (var day = 14; day <= 20; day++)
    {
      db.Transactions.Add(new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = food.Id,
        OccurredAt = new DateTimeOffset(2025, 02, day, 12, 0, 0, TimeSpan.Zero),
        Description = "Food",
        Amount = -10m,
        Currency = "BRL",
        Fingerprint = $"g{day}"
      });
    }

    db.Transactions.Add(new Transaction
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      AccountId = accountId,
      CategoryId = food.Id,
      OccurredAt = new DateTimeOffset(2025, 02, 19, 12, 0, 0, TimeSpan.Zero),
      Description = "Ignored",
      Amount = -1000m,
      IgnoreInDashboard = true,
      Currency = "BRL",
      Fingerprint = "ign"
    });

    await db.SaveChangesAsync(CancellationToken.None);

    var svc = new ComputeForecastService(db, clock);
    var forecast = await svc.ComputeAsync(userId, new DateOnly(2025, 02, 01), CancellationToken.None);

    Assert.Equal(new DateOnly(2025, 02, 01), forecast.Month);
    Assert.Equal(new DateOnly(2025, 02, 20), forecast.AsOfDate);
    Assert.Equal(105m, forecast.TotalSpentToDate);
    Assert.Equal(185m, forecast.TotalProjected); // 105 + (10 * 8 remaining days)

    var cat = Assert.Single(forecast.Categories);
    Assert.Equal(food.Id, cat.CategoryId);
    Assert.Equal(105m, cat.SpentToDate);
    Assert.Equal(185m, cat.ProjectedTotal);
    Assert.True(cat.RiskOfExceedingBudget);
  }

  [Fact]
  public async Task Forecast_falls_back_to_month_to_date_average_when_less_than_3_transaction_days_30_days()
  {
    await using var db = CreateDb();
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 04, 02, 10, 0, 0, TimeSpan.Zero) };
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();

    var leisure = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Lazer" };
    db.Categories.Add(leisure);
    db.Budgets.Add(new Budget
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      CategoryId = leisure.Id,
      Month = new DateOnly(2025, 04, 01),
      LimitAmount = 700m
    });

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = leisure.Id,
        OccurredAt = new DateTimeOffset(2025, 04, 01, 12, 0, 0, TimeSpan.Zero),
        Description = "Lazer",
        Amount = -20m,
        Currency = "BRL",
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = leisure.Id,
        OccurredAt = new DateTimeOffset(2025, 04, 02, 9, 0, 0, TimeSpan.Zero),
        Description = "Lazer",
        Amount = -20m,
        Currency = "BRL",
        Fingerprint = "t2"
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var svc = new ComputeForecastService(db, clock);
    var forecast = await svc.ComputeAsync(userId, new DateOnly(2025, 04, 15), CancellationToken.None);

    Assert.Equal(new DateOnly(2025, 04, 01), forecast.Month);
    Assert.Equal(new DateOnly(2025, 04, 02), forecast.AsOfDate);
    Assert.Equal(40m, forecast.TotalSpentToDate);
    Assert.Equal(600m, forecast.TotalProjected); // avg month-to-date = 20/day, remaining 28 days

    var cat = Assert.Single(forecast.Categories);
    Assert.False(cat.RiskOfExceedingBudget);
  }

  [Fact]
  public async Task Forecast_for_past_month_projects_equal_to_actual_and_includes_budget_with_zero_spend_31_days()
  {
    await using var db = CreateDb();
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 02, 05, 0, 0, 0, TimeSpan.Zero) };
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();

    var a = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "A" };
    var b = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "B" };
    db.Categories.AddRange(a, b);

    db.Budgets.AddRange(
      new Budget
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        CategoryId = a.Id,
        Month = new DateOnly(2025, 01, 01),
        LimitAmount = 999m
      },
      new Budget
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        CategoryId = b.Id,
        Month = new DateOnly(2025, 01, 01),
        LimitAmount = 50m
      });

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = a.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
        Description = "A1",
        Amount = -10m,
        Currency = "BRL",
        Fingerprint = "a1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = a.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 02, 0, 0, 0, TimeSpan.Zero),
        Description = "A2",
        Amount = -10m,
        Currency = "BRL",
        Fingerprint = "a2"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = a.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 10, 0, 0, 0, TimeSpan.Zero),
        Description = "Income",
        Amount = 123m,
        Currency = "BRL",
        Fingerprint = "inc"
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var svc = new ComputeForecastService(db, clock);
    var forecast = await svc.ComputeAsync(userId, new DateOnly(2025, 01, 01), CancellationToken.None);

    Assert.Equal(new DateOnly(2025, 01, 31), forecast.AsOfDate);
    Assert.Equal(20m, forecast.TotalSpentToDate);
    Assert.Equal(20m, forecast.TotalProjected);

    Assert.Equal(2, forecast.Categories.Count);
    Assert.Contains(forecast.Categories, c => c.CategoryId == b.Id && c.SpentToDate == 0m && c.ProjectedTotal == 0m);
  }
}

