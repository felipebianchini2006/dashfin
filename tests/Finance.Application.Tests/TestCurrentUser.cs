using Finance.Application.Abstractions;

namespace Finance.Application.Tests;

internal sealed class TestCurrentUser : ICurrentUser
{
  public Guid? UserId { get; set; }
  public bool IsAuthenticated => UserId is not null;
  public string? Email { get; set; }
}

