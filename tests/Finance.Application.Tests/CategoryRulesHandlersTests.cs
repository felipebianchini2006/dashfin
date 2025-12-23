using Finance.Application.CategoryRules;
using Finance.Application.CategoryRules.Create;
using Finance.Application.Imports.Processing;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class CategoryRulesHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task AutoCategorizer_picks_best_rule_by_priority()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();

    var cat1 = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Transporte" };
    var cat2 = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Outros" };
    db.Categories.AddRange(cat1, cat2);

    db.CategoryRules.AddRange(
      new CategoryRule
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        CategoryId = cat1.Id,
        MatchType = CategoryRuleMatchType.Contains,
        Pattern = "UBER",
        Priority = 50,
        IsActive = true
      },
      new CategoryRule
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        CategoryId = cat2.Id,
        MatchType = CategoryRuleMatchType.Contains,
        Pattern = "UBER",
        Priority = 10,
        IsActive = true
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var categorizer = new CategoryAutoCategorizer(db);
    var compiled = await categorizer.LoadAsync(userId, CancellationToken.None);

    var desc = DescriptionNormalizer.Normalize("Uber Trip");
    var matched = compiled.MatchCategoryId(accountId, desc, -10m);

    Assert.Equal(cat2.Id, matched);
  }

  [Fact]
  public async Task AutoCategorizer_supports_regex_with_timeout_and_does_not_throw()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var accountId = Guid.NewGuid();

    var cat = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Alimentação" };
    db.Categories.Add(cat);
    db.CategoryRules.Add(new CategoryRule
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      CategoryId = cat.Id,
      MatchType = CategoryRuleMatchType.Regex,
      Pattern = "^IFOOD\\b",
      Priority = 1,
      IsActive = true
    });

    await db.SaveChangesAsync(CancellationToken.None);

    var categorizer = new CategoryAutoCategorizer(db);
    var compiled = await categorizer.LoadAsync(userId, CancellationToken.None);

    var matched = compiled.MatchCategoryId(accountId, DescriptionNormalizer.Normalize("iFood"), -25m);
    Assert.Equal(cat.Id, matched);
  }

  [Fact]
  public async Task CreateCategoryRule_normalizes_contains_pattern_and_validates_regex()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var cat = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Transporte" };
    db.Categories.Add(cat);
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new CreateCategoryRuleCommandHandler(db, new TestCurrentUser { UserId = userId });
    var created = await handler.Handle(new CreateCategoryRuleCommand("  uber  trip  ", CategoryRuleMatchType.Contains, cat.Id, 100, true), CancellationToken.None);

    Assert.True(created.IsSuccess);
    Assert.Equal("UBER TRIP", created.Value!.Pattern);

    var invalidRegex = await handler.Handle(new CreateCategoryRuleCommand("(", CategoryRuleMatchType.Regex, cat.Id, 100, true), CancellationToken.None);
    Assert.True(invalidRegex.IsFailure);
    Assert.Equal("validation_error", invalidRegex.Error!.Code);
  }
}

