using Finance.Application.Budgets.Get;
using Finance.Application.Budgets.Upsert;
using Finance.Domain.Entities;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class BudgetsHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task UpsertBudget_creates_then_updates_same_month_category()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Alimentação" };
    db.Categories.Add(category);
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new UpsertBudgetCommandHandler(db, new TestCurrentUser { UserId = userId });
    var month = new DateOnly(2025, 01, 15); // should normalize to 1st

    var created = await handler.Handle(new UpsertBudgetCommand(category.Id, month, 100m), CancellationToken.None);
    Assert.True(created.IsSuccess);
    Assert.Equal(new DateOnly(2025, 01, 01), created.Value!.Month);
    Assert.Equal(100m, created.Value.LimitAmount);

    var updated = await handler.Handle(new UpsertBudgetCommand(category.Id, new DateOnly(2025, 01, 01), 200m), CancellationToken.None);
    Assert.True(updated.IsSuccess);
    Assert.Equal(200m, updated.Value!.LimitAmount);
    Assert.Equal(1, await db.Budgets.CountAsync());
  }

  [Fact]
  public async Task GetBudgets_includes_monthly_spend_negative_only_ignoring_dashboard()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Lazer" };
    db.Categories.Add(category);
    db.Budgets.Add(new Budget
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      CategoryId = category.Id,
      Month = new DateOnly(2025, 02, 01),
      LimitAmount = 300m,
      CreatedAt = new DateTimeOffset(2025, 02, 01, 0, 0, 0, TimeSpan.Zero)
    });

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = Guid.NewGuid(),
        CategoryId = category.Id,
        OccurredAt = new DateTimeOffset(2025, 02, 10, 12, 0, 0, TimeSpan.Zero),
        Description = "Compra 1",
        Amount = -50m,
        Currency = "BRL",
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = Guid.NewGuid(),
        CategoryId = category.Id,
        OccurredAt = new DateTimeOffset(2025, 02, 11, 12, 0, 0, TimeSpan.Zero),
        Description = "Ignored",
        Amount = -20m,
        IgnoreInDashboard = true,
        Currency = "BRL",
        Fingerprint = "t2"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = Guid.NewGuid(),
        CategoryId = category.Id,
        OccurredAt = new DateTimeOffset(2025, 02, 12, 12, 0, 0, TimeSpan.Zero),
        Description = "Income",
        Amount = 999m,
        Currency = "BRL",
        Fingerprint = "t3"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = Guid.NewGuid(),
        CategoryId = category.Id,
        OccurredAt = new DateTimeOffset(2025, 03, 01, 0, 0, 0, TimeSpan.Zero),
        Description = "Other month",
        Amount = -999m,
        Currency = "BRL",
        Fingerprint = "t4"
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new GetBudgetsQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new GetBudgetsQuery(new DateOnly(2025, 02, 28)), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var budget = Assert.Single(result.Value!);
    Assert.Equal(50m, budget.SpentAmount);
  }
}

