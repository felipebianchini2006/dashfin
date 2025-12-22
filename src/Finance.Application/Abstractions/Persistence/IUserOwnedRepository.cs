using Finance.Domain.Common;

namespace Finance.Application.Abstractions.Persistence;

public interface IUserOwnedRepository<TEntity, TKey>
  where TEntity : class, IUserOwnedEntity
{
  Task<TEntity?> GetByIdAsync(Guid userId, TKey id, CancellationToken ct);
  Task AddAsync(TEntity entity, CancellationToken ct);
  void Remove(TEntity entity);
}

