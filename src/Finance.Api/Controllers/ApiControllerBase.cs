using Finance.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
  private readonly ICurrentUser _currentUser;

  protected ApiControllerBase(ICurrentUser currentUser) => _currentUser = currentUser;

  protected Guid UserId =>
    _currentUser.UserId ?? throw new UnauthorizedAccessException("Missing authenticated user.");
}

