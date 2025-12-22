using Finance.Application.Auth.Me;
using Finance.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public sealed class MeController : ControllerBase
{
  private readonly IMediator _mediator;
  public MeController(IMediator mediator) => _mediator = mediator;

  [HttpGet]
  public async Task<IActionResult> Get(CancellationToken ct)
  {
    var result = await _mediator.Send(new GetMeQuery(), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Ok(result.Value);
  }
}

