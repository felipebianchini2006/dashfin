using System.Security.Claims;
using Finance.Application.Abstractions;

namespace Finance.Api.Auth;

public sealed class CurrentUser : ICurrentUser
{
  private readonly IHttpContextAccessor _http;
  public CurrentUser(IHttpContextAccessor http) => _http = http;

  public Guid? UserId
  {
    get
    {
      var sub = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User?.FindFirstValue("sub");
      return Guid.TryParse(sub, out var id) ? id : null;
    }
  }

  public bool IsAuthenticated => UserId is not null;
  public string? Email => _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? _http.HttpContext?.User?.FindFirstValue("email");
}

