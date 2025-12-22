using Finance.Application.Abstractions.Persistence;
using Finance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : ITransactionRepository
{
  private readonly FinanceDbContext _db;

  public TransactionRepository(FinanceDbContext db) => _db = db;

  public Task<Transaction?> GetByIdAsync(Guid userId, Guid id, CancellationToken ct) =>
    _db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Id == id, ct);

  public Task<Transaction?> GetByFingerprintAsync(Guid userId, string fingerprint, CancellationToken ct) =>
    _db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.Fingerprint == fingerprint, ct);

  public Task AddAsync(Transaction entity, CancellationToken ct) => _db.Transactions.AddAsync(entity, ct).AsTask();

  public void Remove(Transaction entity) => _db.Transactions.Remove(entity);
}

