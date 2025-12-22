namespace Finance.Domain.Common;

public interface IUserOwnedEntity
{
  Guid UserId { get; set; }
}

