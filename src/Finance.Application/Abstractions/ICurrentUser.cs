namespace Finance.Application.Abstractions;

public interface ICurrentUser
{
  Guid? UserId { get; }
  bool IsAuthenticated { get; }
  string? Email { get; }
}

