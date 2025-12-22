namespace Finance.Domain.Common;

public abstract class BaseEntity<TKey>
{
  public TKey Id { get; set; } = default!;
  public DateTimeOffset CreatedAt { get; set; }
}
