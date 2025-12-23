using Finance.Application.Transactions.List;
using Finance.Application.Transactions.Models;
using Finance.Application.Transactions.Update;
using Finance.Domain.Entities;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class TransactionsHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task ListTransactions_applies_filters_search_and_pagination_and_sorts_desc()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var account1 = Guid.NewGuid();
    var account2 = Guid.NewGuid();
    var catFood = Guid.NewGuid();
    var catTransport = Guid.NewGuid();

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = account1,
        CategoryId = catTransport,
        OccurredAt = new DateTimeOffset(2025, 01, 12, 10, 0, 0, TimeSpan.Zero),
        Description = "Uber Trip",
        Notes = "WORK",
        Amount = -50m,
        Currency = "BRL",
        Fingerprint = "t1",
        CreatedAt = new DateTimeOffset(2025, 01, 12, 10, 0, 1, TimeSpan.Zero)
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = account1,
        CategoryId = catFood,
        OccurredAt = new DateTimeOffset(2025, 01, 11, 10, 0, 0, TimeSpan.Zero),
        Description = "iFood",
        Notes = "delivery",
        Amount = -20m,
        Currency = "BRL",
        Fingerprint = "t2",
        CreatedAt = new DateTimeOffset(2025, 01, 11, 10, 0, 1, TimeSpan.Zero)
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = account2,
        CategoryId = null,
        OccurredAt = new DateTimeOffset(2025, 01, 10, 10, 0, 0, TimeSpan.Zero),
        Description = "Salary",
        Notes = null,
        Amount = 100m,
        Currency = "BRL",
        Fingerprint = "t3",
        CreatedAt = new DateTimeOffset(2025, 01, 10, 10, 0, 1, TimeSpan.Zero)
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = otherUserId,
        AccountId = account1,
        CategoryId = catTransport,
        OccurredAt = new DateTimeOffset(2025, 01, 12, 10, 0, 0, TimeSpan.Zero),
        Description = "Uber Trip",
        Notes = null,
        Amount = -50m,
        Currency = "BRL",
        Fingerprint = "x1",
        CreatedAt = new DateTimeOffset(2025, 01, 12, 10, 0, 1, TimeSpan.Zero)
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new ListTransactionsQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new ListTransactionsQuery(
      From: new DateTimeOffset(2025, 01, 10, 0, 0, 0, TimeSpan.Zero),
      To: new DateTimeOffset(2025, 01, 12, 23, 59, 59, TimeSpan.Zero),
      AccountId: account1,
      CategoryId: null,
      Type: TransactionFlow.Saida,
      MinAmount: 30m,
      MaxAmount: 60m,
      Search: "uber",
      Page: 1,
      PageSize: 10), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var response = result.Value!;
    Assert.Equal(1, response.TotalCount);
    var item = Assert.Single(response.Items);
    Assert.Equal(TransactionFlow.Saida, item.Type);
    Assert.Equal(account1, item.AccountId);
    Assert.Equal("Uber Trip", item.Description);

    var handler2 = new ListTransactionsQueryHandler(db, new TestCurrentUser { UserId = userId });
    var paged = await handler2.Handle(new ListTransactionsQuery(
      From: null,
      To: null,
      AccountId: null,
      CategoryId: null,
      Type: null,
      MinAmount: null,
      MaxAmount: null,
      Search: null,
      Page: 1,
      PageSize: 1), CancellationToken.None);

    Assert.True(paged.IsSuccess);
    Assert.Equal(3, paged.Value!.TotalCount);
    Assert.Single(paged.Value.Items);
    Assert.Equal(new DateTimeOffset(2025, 01, 12, 10, 0, 0, TimeSpan.Zero), paged.Value.Items[0].OccurredAt);
  }

  [Fact]
  public async Task ListTransactions_search_matches_notes()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    db.Transactions.Add(new Transaction
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      AccountId = Guid.NewGuid(),
      OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
      Description = "Compra",
      Notes = "Lunch with team",
      Amount = -10m,
      Currency = "BRL",
      Fingerprint = "t1",
      CreatedAt = new DateTimeOffset(2025, 01, 01, 0, 0, 1, TimeSpan.Zero)
    });
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new ListTransactionsQueryHandler(db, new TestCurrentUser { UserId = userId });
    var result = await handler.Handle(new ListTransactionsQuery(null, null, null, null, null, null, null, "lunch"), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Single(result.Value!.Items);
  }

  [Fact]
  public async Task UpdateTransaction_updates_category_notes_and_ignore_and_prevents_cross_user()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();

    var category = new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Food" };
    var otherCategory = new Category { Id = Guid.NewGuid(), UserId = otherUserId, Name = "Other" };
    db.Categories.AddRange(category, otherCategory);

    var tx = new Transaction
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      AccountId = Guid.NewGuid(),
      OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
      Description = "Compra",
      Notes = null,
      IgnoreInDashboard = false,
      Amount = -10m,
      Currency = "BRL",
      Fingerprint = "t1",
      CreatedAt = new DateTimeOffset(2025, 01, 01, 0, 0, 1, TimeSpan.Zero)
    };
    var otherTx = new Transaction
    {
      Id = Guid.NewGuid(),
      UserId = otherUserId,
      AccountId = Guid.NewGuid(),
      OccurredAt = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
      Description = "Other",
      Amount = -10m,
      Currency = "BRL",
      Fingerprint = "o1",
      CreatedAt = new DateTimeOffset(2025, 01, 01, 0, 0, 1, TimeSpan.Zero)
    };

    db.Transactions.AddRange(tx, otherTx);
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new UpdateTransactionCommandHandler(db, new TestCurrentUser { UserId = userId });
    var updated = await handler.Handle(new UpdateTransactionCommand(tx.Id, category.Id, "  hello  ", true), CancellationToken.None);

    Assert.True(updated.IsSuccess);
    var dto = updated.Value!;
    Assert.Equal(category.Id, dto.CategoryId);
    Assert.Equal("hello", dto.Notes);
    Assert.True(dto.IgnoreInDashboard);

    var cleared = await handler.Handle(new UpdateTransactionCommand(tx.Id, Guid.Empty, "", false), CancellationToken.None);
    Assert.True(cleared.IsSuccess);
    Assert.Null(cleared.Value!.CategoryId);
    Assert.Null(cleared.Value!.Notes);
    Assert.False(cleared.Value!.IgnoreInDashboard);

    var crossUser = await handler.Handle(new UpdateTransactionCommand(otherTx.Id, category.Id, "x", null), CancellationToken.None);
    Assert.True(crossUser.IsFailure);
    Assert.Equal("not_found", crossUser.Error!.Code);

    var invalidCategory = await handler.Handle(new UpdateTransactionCommand(tx.Id, otherCategory.Id, "x", null), CancellationToken.None);
    Assert.True(invalidCategory.IsFailure);
    Assert.Equal("validation_error", invalidCategory.Error!.Code);
  }

  [Fact]
  public async Task Validators_reject_invalid_ranges_and_missing_patch_fields()
  {
    var listValidator = new ListTransactionsQueryValidator();
    var badRange = await listValidator.ValidateAsync(new ListTransactionsQuery(
      From: new DateTimeOffset(2025, 01, 02, 0, 0, 0, TimeSpan.Zero),
      To: new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero),
      AccountId: null,
      CategoryId: null,
      Type: null,
      MinAmount: null,
      MaxAmount: null,
      Search: null));
    Assert.False(badRange.IsValid);

    var patchValidator = new UpdateTransactionCommandValidator();
    var missingFields = await patchValidator.ValidateAsync(new UpdateTransactionCommand(Guid.NewGuid(), null, null, null));
    Assert.False(missingFields.IsValid);
  }
}

