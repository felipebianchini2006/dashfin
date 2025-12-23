using Finance.Application.Accounts.Create;
using Finance.Application.Accounts.List;
using Finance.Application.Accounts.Update;
using Finance.Application.Common.Exceptions;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
  private readonly IMediator _mediator;
  public AccountsController(IMediator mediator) => _mediator = mediator;

  public sealed record CreateAccountRequest(string Name, AccountType Type, decimal? InitialBalance);
  public sealed record UpdateAccountRequest(string? Name, AccountType? Type, decimal? InitialBalance);

  [HttpGet]
  public async Task<IActionResult> List(CancellationToken ct)
  {
    var result = await _mediator.Send(new ListAccountsQuery(), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new CreateAccountCommand(request.Name, request.Type, request.InitialBalance), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Created($"/accounts/{result.Value!.Id}", result.Value);
  }

  [HttpPatch("{id:guid}")]
  public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateAccountRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new UpdateAccountCommand(id, request.Name, request.Type, request.InitialBalance), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }
}
