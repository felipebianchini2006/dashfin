using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Finance.Application.Abstractions;

public interface IAppDbContext : Finance.Application.Abstractions.Persistence.IUnitOfWork
{
  DbSet<User> Users { get; }
  DbSet<UserRefreshToken> UserRefreshTokens { get; }
  DbSet<Account> Accounts { get; }
  DbSet<ImportBatch> Imports { get; }
  DbSet<ImportRow> ImportRows { get; }
  DbSet<Category> Categories { get; }
  DbSet<CategoryRule> CategoryRules { get; }
  DbSet<Transaction> Transactions { get; }
  DbSet<Budget> Budgets { get; }
  DbSet<AlertRule> AlertRules { get; }
  DbSet<AlertEvent> AlertEvents { get; }

  DatabaseFacade Database { get; }
  Task<int> SaveChangesAsync(CancellationToken ct);
}
