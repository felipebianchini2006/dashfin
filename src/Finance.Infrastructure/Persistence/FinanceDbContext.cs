using Finance.Application.Abstractions;
using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Persistence;

public sealed class FinanceDbContext : DbContext, IAppDbContext
{
  public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

  public DbSet<User> Users => Set<User>();
  public DbSet<UserRefreshToken> UserRefreshTokens => Set<UserRefreshToken>();
  public DbSet<Account> Accounts => Set<Account>();
  public DbSet<ImportBatch> Imports => Set<ImportBatch>();
  public DbSet<ImportRow> ImportRows => Set<ImportRow>();
  public DbSet<Category> Categories => Set<Category>();
  public DbSet<CategoryRule> CategoryRules => Set<CategoryRule>();
  public DbSet<Transaction> Transactions => Set<Transaction>();
  public DbSet<Budget> Budgets => Set<Budget>();
  public DbSet<AlertRule> AlertRules => Set<AlertRule>();
  public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();

  public Task<int> SaveChangesAsync(CancellationToken ct) => base.SaveChangesAsync(ct);

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinanceDbContext).Assembly);
  }
}
