using System.Text.Json.Serialization;
using Finance.Application.Alerts.List;
using Finance.Application.Alerts.Update;
using Finance.Application.Common.Exceptions;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("alerts")]
[Authorize]
public sealed class AlertsController : ControllerBase
{
  private readonly IMediator _mediator;
  public AlertsController(IMediator mediator) => _mediator = mediator;

  public sealed class UpdateAlertRequest
  {
    [JsonPropertyName("status")]
    public AlertEventStatus Status { get; init; }
  }

  [HttpGet]
  public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
  {
    AlertEventStatus? parsed = null;
    if (!string.IsNullOrWhiteSpace(status))
    {
      if (!Enum.TryParse<AlertEventStatus>(status, ignoreCase: true, out var s))
        throw new AppException(Finance.Application.Common.Error.Validation("Invalid status."));
      parsed = s;
    }

    var result = await _mediator.Send(new ListAlertsQuery(parsed), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpPatch("{id:guid}")]
  public async Task<IActionResult> Patch([FromRoute] Guid id, [FromBody] UpdateAlertRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new UpdateAlertStatusCommand(id, request.Status), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok();
  }
}

