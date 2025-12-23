using Finance.Application.Dashboard.Balances;
using Finance.Application.Dashboard.Categories;
using Finance.Application.Dashboard.Summary;
using Finance.Application.Dashboard.Timeseries;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class DashboardHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task Summary_returns_income_checking_out_card_spend_netcash_top_categories_and_budget_progress()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();

    var checking = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AccountType.Checking,
      Name = "Banco",
      Currency = "BRL",
      InitialBalance = 100m
    };
    var card = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AccountType.CreditCard,
      Name = "Cartão",
      Currency = "BRL"
    };
    db.Accounts.AddRange(checking, card);

    var food = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Alimentação" };
    var transport = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Transporte" };
    db.Categories.AddRange(food, transport);

    db.Budgets.Add(new Budget
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      CategoryId = food.Id,
      Month = new DateOnly(2025, 01, 01),
      LimitAmount = 100m
    });

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        CategoryId = null,
        OccurredAt = new DateTimeOffset(2025, 01, 05, 0, 0, 0, TimeSpan.Zero),
        Description = "Salary",
        Amount = 500m,
        Currency = "BRL",
        Fingerprint = "i1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        CategoryId = food.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 06, 0, 0, 0, TimeSpan.Zero),
        Description = "Food",
        Amount = -40m,
        Currency = "BRL",
        Fingerprint = "e1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        CategoryId = transport.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 07, 0, 0, 0, TimeSpan.Zero),
        Description = "Uber",
        Amount = -30m,
        Currency = "BRL",
        Fingerprint = "e2"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = card.Id,
        CategoryId = food.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 08, 0, 0, 0, TimeSpan.Zero),
        Description = "Card food",
        Amount = -80m,
        Currency = "BRL",
        Fingerprint = "c1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        CategoryId = food.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 09, 0, 0, 0, TimeSpan.Zero),
        Description = "Ignored",
        Amount = -999m,
        IgnoreInDashboard = true,
        Currency = "BRL",
        Fingerprint = "ign"
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new GetDashboardSummaryQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new GetDashboardSummaryQuery(new DateOnly(2025, 01, 15)), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var s = result.Value!;
    Assert.Equal(new DateOnly(2025, 01, 01), s.Month);
    Assert.Equal(500m, s.IncomeAmount);
    Assert.Equal(70m, s.CheckingOutAmount);
    Assert.Equal(80m, s.CreditCardSpendAmount);
    Assert.Equal(430m, s.NetCashAmount); // 500 - 40 - 30

    Assert.Equal(food.Id, s.TopCategories[0].CategoryId);
    Assert.Equal(120m, s.TopCategories[0].SpentAmount); // 40 + 80

    var bp = Assert.Single(s.BudgetProgress);
    Assert.Equal(food.Id, bp.CategoryId);
    Assert.Equal(120m, bp.SpentAmount);
    Assert.Equal(100m, bp.LimitAmount);
    Assert.True(bp.IsOverBudget);
  }

  [Fact]
  public async Task Categories_distribution_returns_all_expense_categories()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();

    var a = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "A" };
    var b = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "B" };
    db.Categories.AddRange(a, b);

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = a.Id,
        OccurredAt = new DateTimeOffset(2025, 03, 01, 0, 0, 0, TimeSpan.Zero),
        Description = "A",
        Amount = -10m,
        Currency = "BRL",
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = b.Id,
        OccurredAt = new DateTimeOffset(2025, 03, 02, 0, 0, 0, TimeSpan.Zero),
        Description = "B",
        Amount = -20m,
        Currency = "BRL",
        Fingerprint = "t2"
      });
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new GetDashboardCategoriesQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new GetDashboardCategoriesQuery(new DateOnly(2025, 03, 10)), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.Value!.Items.Count);
    Assert.Equal(b.Id, result.Value.Items[0].CategoryId);
    Assert.Equal(20m, result.Value.Items[0].SpentAmount);
  }

  [Fact]
  public async Task Timeseries_fills_missing_days_and_excludes_ignore_in_dashboard()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = null,
        OccurredAt = new DateTimeOffset(2025, 04, 01, 0, 0, 0, TimeSpan.Zero),
        Description = "d1",
        Amount = -10m,
        Currency = "BRL",
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = null,
        OccurredAt = new DateTimeOffset(2025, 04, 03, 0, 0, 0, TimeSpan.Zero),
        Description = "d3",
        Amount = -20m,
        Currency = "BRL",
        Fingerprint = "t2"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = accountId,
        CategoryId = null,
        OccurredAt = new DateTimeOffset(2025, 04, 03, 0, 0, 0, TimeSpan.Zero),
        Description = "ignored",
        Amount = -999m,
        IgnoreInDashboard = true,
        Currency = "BRL",
        Fingerprint = "ign"
      });
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new GetDashboardTimeseriesQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new GetDashboardTimeseriesQuery(new DateOnly(2025, 04, 01)), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(30, result.Value!.Items.Count);
    Assert.Equal(10m, result.Value.Items[0].SpentAmount);
    Assert.Equal(0m, result.Value.Items[1].SpentAmount);
    Assert.Equal(20m, result.Value.Items[2].SpentAmount);
  }

  [Fact]
  public async Task Balances_returns_checking_balances_savings_total_and_net_worth_with_card_open()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();

    var checking = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AccountType.Checking,
      Name = "Banco",
      Currency = "BRL",
      InitialBalance = 100m
    };
    var savings = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AccountType.Savings,
      Name = "Poupança",
      Currency = "BRL",
      InitialBalance = 50m
    };
    var card = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AccountType.CreditCard,
      Name = "Cartão",
      Currency = "BRL"
    };
    db.Accounts.AddRange(checking, savings, card);

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
        Description = "in",
        Amount = 20m,
        Currency = "BRL",
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = savings.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
        Description = "save",
        Amount = 10m,
        Currency = "BRL",
        Fingerprint = "t2"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = card.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
        Description = "card",
        Amount = -30m,
        Currency = "BRL",
        Fingerprint = "t3"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 02, 0, 0, 0, TimeSpan.Zero),
        Description = "ignored",
        Amount = -999m,
        IgnoreInDashboard = true,
        Currency = "BRL",
        Fingerprint = "ign"
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new GetDashboardBalancesQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new GetDashboardBalancesQuery(), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var dto = result.Value!;
    Assert.Single(dto.CheckingAccounts);
    Assert.Equal(120m, dto.CheckingAccounts[0].Balance); // 100 + 20
    Assert.Equal(60m, dto.TotalSaved); // 50 + 10
    Assert.Equal(30m, dto.CreditCardOpen);
    Assert.Equal(150m, dto.NetWorth); // 120 + 60 - 30
  }
}

