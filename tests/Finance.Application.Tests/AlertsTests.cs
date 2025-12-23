using Finance.Application.Alerts.Generate;
using Finance.Application.Alerts.List;
using Finance.Application.Alerts.Update;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class AlertsTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task GenerateAlerts_is_idempotent_and_creates_overbudget_warning_and_critical()
  {
    await using var db = CreateDb();
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 02, 15, 0, 0, 0, TimeSpan.Zero) };

    var userId = Guid.NewGuid();
    var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Alimentação" };
    db.Categories.Add(category);

    db.Budgets.Add(new Budget
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      CategoryId = category.Id,
      Month = new DateOnly(2025, 02, 01),
      LimitAmount = 100m
    });

    var rule = new AlertRule
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AlertRuleType.OverBudget,
      Name = "Over budget",
      IsActive = true,
      CategoryId = category.Id
    };
    db.AlertRules.Add(rule);

    db.Transactions.Add(new Transaction
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      AccountId = Guid.NewGuid(),
      CategoryId = category.Id,
      OccurredAt = new DateTimeOffset(2025, 02, 10, 0, 0, 0, TimeSpan.Zero),
      Description = "Compra",
      Amount = -120m,
      IgnoreInDashboard = false,
      Currency = "BRL",
      Fingerprint = "t1"
    });
    await db.SaveChangesAsync(CancellationToken.None);

    var svc = new GenerateAlertsService(db, clock);
    await svc.GenerateAsync(userId, 2025, 2, CancellationToken.None);
    await svc.GenerateAsync(userId, 2025, 2, CancellationToken.None);

    var events = await db.AlertEvents.Where(a => a.UserId == userId).ToListAsync();
    Assert.Equal(2, events.Count);
    Assert.All(events, e => Assert.False(string.IsNullOrWhiteSpace(e.Fingerprint)));
  }

  [Fact]
  public async Task GenerateAlerts_creates_threshold_events_and_ignores_dashboard_transactions()
  {
    await using var db = CreateDb();
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 03, 15, 0, 0, 0, TimeSpan.Zero) };

    var userId = Guid.NewGuid();
    var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Lazer" };
    db.Categories.Add(category);

    var rule = new AlertRule
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AlertRuleType.Threshold,
      Name = "Threshold",
      IsActive = true,
      CategoryId = category.Id,
      ThresholdAmount = 50m
    };
    db.AlertRules.Add(rule);

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = Guid.NewGuid(),
        CategoryId = category.Id,
        OccurredAt = new DateTimeOffset(2025, 03, 10, 0, 0, 0, TimeSpan.Zero),
        Description = "Compra",
        Amount = -40m,
        Currency = "BRL",
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = Guid.NewGuid(),
        CategoryId = category.Id,
        OccurredAt = new DateTimeOffset(2025, 03, 11, 0, 0, 0, TimeSpan.Zero),
        Description = "Ignored",
        Amount = -999m,
        IgnoreInDashboard = true,
        Currency = "BRL",
        Fingerprint = "t2"
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var svc = new GenerateAlertsService(db, clock);
    await svc.GenerateAsync(userId, 2025, 3, CancellationToken.None);

    var events = await db.AlertEvents.Where(a => a.UserId == userId).ToListAsync();
    Assert.Single(events);
    Assert.Contains("Limite perto do máximo", events[0].Title);
  }

  [Fact]
  public async Task Alerts_handlers_list_by_status_and_patch_status_cross_user_safe()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();

    var ruleId = Guid.NewGuid();
    db.AlertEvents.AddRange(
      new AlertEvent
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AlertRuleId = ruleId,
        Fingerprint = new string('a', 64),
        Status = AlertEventStatus.New,
        OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
        Title = "t",
        Body = null
      },
      new AlertEvent
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AlertRuleId = ruleId,
        Fingerprint = new string('b', 64),
        Status = AlertEventStatus.Read,
        OccurredAt = new DateTimeOffset(2025, 01, 02, 0, 0, 0, TimeSpan.Zero),
        Title = "t2",
        Body = null
      },
      new AlertEvent
      {
        Id = Guid.NewGuid(),
        UserId = otherUserId,
        AlertRuleId = ruleId,
        Fingerprint = new string('c', 64),
        Status = AlertEventStatus.New,
        OccurredAt = new DateTimeOffset(2025, 01, 03, 0, 0, 0, TimeSpan.Zero),
        Title = "x",
        Body = null
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var listHandler = new ListAlertsQueryHandler(db, new TestCurrentUser { UserId = userId });
    var listed = await listHandler.Handle(new ListAlertsQuery(AlertEventStatus.New), CancellationToken.None);
    Assert.True(listed.IsSuccess);
    Assert.Single(listed.Value!);

    var targetId = listed.Value![0].Id;
    var patchHandler = new UpdateAlertStatusCommandHandler(db, new TestCurrentUser { UserId = userId });
    var patched = await patchHandler.Handle(new UpdateAlertStatusCommand(targetId, AlertEventStatus.Dismissed), CancellationToken.None);
    Assert.True(patched.IsSuccess);

    var crossUser = await patchHandler.Handle(new UpdateAlertStatusCommand(db.AlertEvents.Single(a => a.UserId == otherUserId).Id, AlertEventStatus.Read), CancellationToken.None);
    Assert.True(crossUser.IsFailure);
    Assert.Equal("not_found", crossUser.Error!.Code);
  }
}

