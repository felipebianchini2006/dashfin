using Finance.Domain.Entities;

namespace Finance.Application.Abstractions.Persistence;

public interface ITransactionRepository : IUserOwnedRepository<Transaction, Guid>
{
  Task<Transaction?> GetByFingerprintAsync(Guid userId, string fingerprint, CancellationToken ct);
}

