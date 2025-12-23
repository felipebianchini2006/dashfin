using Finance.Application.Abstractions;
using Finance.Application.Auth.Login;
using Finance.Application.Auth.Refresh;
using Finance.Application.Auth.Register;
using Finance.Domain.Entities;
using Finance.Infrastructure.Auth;
using Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Finance.Application.Tests;

public sealed class AuthHandlersTests
{
  private static FinanceDbContext CreateDb()
  {
    var opts = new DbContextOptionsBuilder<FinanceDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FinanceDbContext(opts);
  }

  [Fact]
  public async Task Register_happy_path_creates_user_with_defaults()
  {
    await using var db = CreateDb();
    IPasswordHasher passwordHasher = new IdentityPasswordHasher();
    var handler = new RegisterCommandHandler(db, passwordHasher);

    var result = await handler.Handle(new RegisterCommand("a@b.com", "Password1"), CancellationToken.None);

    Assert.True(result.IsSuccess);
    var user = await db.Users.SingleAsync();
    Assert.Equal("a@b.com", user.Email);
    Assert.Equal("America/Sao_Paulo", user.Timezone);
    Assert.Equal("BRL", user.Currency);
    Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
    Assert.NotEqual("Password1", user.PasswordHash);

    var categories = await db.Categories.Where(c => c.UserId == user.Id).Select(c => c.Name).ToListAsync();
    Assert.Equal(12, categories.Count);
    Assert.Contains("Alimentação", categories);
    Assert.Contains("Transferências/Interno", categories);
  }

  [Fact]
  public async Task Register_invalid_email_returns_validation_error()
  {
    var validator = new RegisterCommandValidator();
    var result = await validator.ValidateAsync(new RegisterCommand("not-an-email", "Password1"));
    Assert.False(result.IsValid);
  }

  [Fact]
  public async Task Login_happy_path_returns_tokens_and_persists_refresh_token()
  {
    await using var db = CreateDb();
    IPasswordHasher passwordHasher = new IdentityPasswordHasher();
    ITokenService tokenService = new TestTokenService();
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero) };
    var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 30 });

    var user = new User { Id = Guid.NewGuid(), Email = "a@b.com" };
    user.PasswordHash = passwordHasher.HashPassword(user, "Password1");
    db.Users.Add(user);
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new LoginCommandHandler(db, passwordHasher, tokenService, clock, jwtOptions);
    var result = await handler.Handle(new LoginCommand("a@b.com", "Password1"), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.StartsWith("access:", result.Value!.AccessToken);
    Assert.Equal("refresh-1", result.Value.RefreshToken);
    Assert.Equal(clock.UtcNow.AddDays(30), result.Value.RefreshTokenExpiresAt);
    Assert.Single(db.UserRefreshTokens);
  }

  [Fact]
  public async Task Login_invalid_password_returns_unauthorized()
  {
    await using var db = CreateDb();
    IPasswordHasher passwordHasher = new IdentityPasswordHasher();
    ITokenService tokenService = new TestTokenService();
    var clock = new TestClock();
    var jwtOptions = Options.Create(new JwtOptions());

    var user = new User { Id = Guid.NewGuid(), Email = "a@b.com" };
    user.PasswordHash = passwordHasher.HashPassword(user, "Password1");
    db.Users.Add(user);
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new LoginCommandHandler(db, passwordHasher, tokenService, clock, jwtOptions);
    var result = await handler.Handle(new LoginCommand("a@b.com", "wrong"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("unauthorized", result.Error!.Code);
  }

  [Fact]
  public async Task Refresh_happy_path_rotates_refresh_token()
  {
    await using var db = CreateDb();
    ITokenService tokenService = new TestTokenService();
    var clock = new TestClock { UtcNow = new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero) };
    var jwtOptions = Options.Create(new JwtOptions { RefreshTokenDays = 30 });

    var user = new User { Id = Guid.NewGuid(), Email = "a@b.com", PasswordHash = "x" };
    db.Users.Add(user);

    var oldRefreshRaw = "refresh-0";
    var oldHash = tokenService.HashRefreshToken(oldRefreshRaw);
    db.UserRefreshTokens.Add(new UserRefreshToken
    {
      Id = Guid.NewGuid(),
      UserId = user.Id,
      TokenHash = oldHash,
      ExpiresAt = clock.UtcNow.AddDays(1)
    });
    await db.SaveChangesAsync(CancellationToken.None);

    var handler = new RefreshCommandHandler(db, tokenService, clock, jwtOptions);
    var result = await handler.Handle(new RefreshCommand(oldRefreshRaw), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("refresh-1", result.Value!.RefreshToken);

    var oldToken = await db.UserRefreshTokens.SingleAsync(t => t.TokenHash == oldHash);
    Assert.NotNull(oldToken.RevokedAt);
    Assert.Equal("rotated", oldToken.RevokedReason);
    Assert.Equal(tokenService.HashRefreshToken("refresh-1"), oldToken.ReplacedByTokenHash);

    var newHash = tokenService.HashRefreshToken("refresh-1");
    Assert.True(await db.UserRefreshTokens.AnyAsync(t => t.TokenHash == newHash));
  }

  [Fact]
  public async Task Refresh_invalid_token_returns_unauthorized()
  {
    await using var db = CreateDb();
    ITokenService tokenService = new TestTokenService();
    var clock = new TestClock();
    var jwtOptions = Options.Create(new JwtOptions());

    var handler = new RefreshCommandHandler(db, tokenService, clock, jwtOptions);
    var result = await handler.Handle(new RefreshCommand("missing"), CancellationToken.None);

    Assert.True(result.IsFailure);
    Assert.Equal("unauthorized", result.Error!.Code);
  }
}
