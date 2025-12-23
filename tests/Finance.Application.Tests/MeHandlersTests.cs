using Finance.Application.Auth.Me;
using Finance.Application.Auth.Update;
using Finance.Domain.Entities;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Application.Tests;

public sealed class MeHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task GetMe_returns_user_profile()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    db.Users.Add(new User { Id = userId, Email = "a@b.com", Timezone = "America/Sao_Paulo", Currency = "BRL" });
    await db.SaveChangesAsync(CancellationToken.None);

    var currentUser = new TestCurrentUser { UserId = userId };
    var handler = new GetMeQueryHandler(db, currentUser);
    var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("a@b.com", result.Value!.Email);
  }

  [Fact]
  public async Task UpdateMe_updates_timezone_currency_and_preferences()
  {
    await using var db = CreateDb();
    var userId = Guid.NewGuid();
    db.Users.Add(new User
    {
      Id = userId,
      Email = "a@b.com",
      Timezone = "America/Sao_Paulo",
      Currency = "BRL",
      DisplayPreferences = new UserDisplayPreferences { Theme = "system", CompactMode = false }
    });
    await db.SaveChangesAsync(CancellationToken.None);

    var currentUser = new TestCurrentUser { UserId = userId };
    var handler = new UpdateMeCommandHandler(db, currentUser);
    var result = await handler.Handle(new UpdateMeCommand("UTC", "usd", "dark", true), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("UTC", result.Value!.Timezone);
    Assert.Equal("USD", result.Value!.Currency);
    Assert.Equal("dark", result.Value!.DisplayPreferences.Theme);
    Assert.True(result.Value!.DisplayPreferences.CompactMode);
  }

  [Fact]
  public async Task UpdateMeValidator_requires_at_least_one_field()
  {
    var validator = new UpdateMeCommandValidator();
    var result = await validator.ValidateAsync(new UpdateMeCommand(null, null, null, null));
    Assert.False(result.IsValid);
  }
}

