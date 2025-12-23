using Finance.Application.Auth.Me;
using Finance.Application.Auth.Update;
using Finance.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Finance.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public sealed class MeController : ControllerBase
{
  private readonly IMediator _mediator;
  public MeController(IMediator mediator) => _mediator = mediator;

  public sealed class UpdateMeRequest
  {
    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("displayPreferences")]
    public DisplayPreferencesRequest? DisplayPreferences { get; init; }

    public sealed class DisplayPreferencesRequest
    {
      [JsonPropertyName("theme")]
      public string? Theme { get; init; }

      [JsonPropertyName("compactMode")]
      public bool? CompactMode { get; init; }
    }
  }

  [HttpGet]
  public async Task<IActionResult> Get(CancellationToken ct)
  {
    var result = await _mediator.Send(new GetMeQuery(), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Ok(result.Value);
  }

  [HttpPatch]
  public async Task<IActionResult> Patch([FromBody] UpdateMeRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new UpdateMeCommand(
      request.Timezone,
      request.Currency,
      request.DisplayPreferences?.Theme,
      request.DisplayPreferences?.CompactMode), ct);

    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Ok(result.Value);
  }
}
