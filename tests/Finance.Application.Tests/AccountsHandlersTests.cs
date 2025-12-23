using Finance.Application.Accounts.Create;
using Finance.Application.Accounts.List;
using Finance.Application.Accounts.Update;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class AccountsHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task CreateAccount_credit_card_forces_initial_balance_zero()
  {
    await using var db = CreateDb();
    var currentUser = new TestCurrentUser { UserId = Guid.NewGuid() };
    var handler = new CreateAccountCommandHandler(db, currentUser);

    var result = await handler.Handle(new CreateAccountCommand("Cartao", AccountType.CreditCard, 123m), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var account = await db.Accounts.SingleAsync();
    Assert.Equal(AccountType.CreditCard, account.Type);
    Assert.Equal(0m, account.InitialBalance);
  }

  [Fact]
  public async Task UpdateAccount_cannot_set_initial_balance_for_credit_card()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var currentUser = new TestCurrentUser { UserId = userId };

    var account = new Account
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      Type = AccountType.CreditCard,
      Name = "Cartao",
      Currency = "BRL",
      InitialBalance = 0m
    };
    db.Accounts.Add(account);
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new UpdateAccountCommandHandler(db, currentUser);
    var result = await handler.Handle(new UpdateAccountCommand(account.Id, null, null, 10m), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("validation_error", result.Error!.Code);
  }

  [Fact]
  public async Task ListAccounts_derived_balance_for_checking_and_monthly_spend_for_credit_card()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    var currentUser = new TestCurrentUser { UserId = userId };
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 01, 15, 12, 0, 0, TimeSpan.Zero) };

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
      Name = "Cartao",
      Currency = "BRL",
      InitialBalance = 0m
    };
    db.Accounts.AddRange(checking, card);

    db.Transactions.AddRange(
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        OccurredAt = clock.UtcNow,
        Description = "Salary",
        Amount = 50m,
        Fingerprint = "t1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = checking.Id,
        OccurredAt = clock.UtcNow,
        Description = "Coffee",
        Amount = -20m,
        Fingerprint = "t2"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = card.Id,
        OccurredAt = new DateTimeOffset(2025, 01, 02, 0, 0, 0, TimeSpan.Zero),
        Description = "Compra 1",
        Amount = -10m,
        Fingerprint = "c1"
      },
      new Transaction
      {
        Id = Guid.NewGuid(),
        UserId = userId,
        AccountId = card.Id,
        OccurredAt = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero),
        Description = "Compra anterior",
        Amount = -999m,
        Fingerprint = "c2"
      });

    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new ListAccountsQueryHandler(db, currentUser, clock);
    var result = await handler.Handle(new ListAccountsQuery(), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var items = result.Value!;
    var checkingDto = Assert.Single(items, x => x.Type == AccountType.Checking);
    Assert.Equal(130m, checkingDto.Balance);

    var cardDto = Assert.Single(items, x => x.Type == AccountType.CreditCard);
    Assert.Null(cardDto.Balance);
    Assert.Equal(10m, cardDto.CreditCardSpendThisMonth);
  }

  [Fact]
  public async Task UpdateAccountValidator_requires_at_least_one_field()
  {
    var validator = new UpdateAccountCommandValidator();
    var result = await validator.ValidateAsync(new UpdateAccountCommand(Guid.NewGuid(), null, null, null));
    Assert.False(result.IsValid);
  }
}
