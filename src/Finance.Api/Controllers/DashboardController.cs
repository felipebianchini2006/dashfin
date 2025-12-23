using Finance.Application.Common.Exceptions;
using Finance.Application.Dashboard.Get;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
  private readonly IMediator _mediator;
  public DashboardController(IMediator mediator) => _mediator = mediator;

  [HttpGet]
  public async Task<IActionResult> Get([FromQuery(Name = "month")] string? month, CancellationToken ct)
  {
    DateOnly parsed;
    if (string.IsNullOrWhiteSpace(month))
    {
      var now = DateOnly.FromDateTime(DateTime.UtcNow);
      parsed = new DateOnly(now.Year, now.Month, 1);
    }
    else if (!DateOnly.TryParseExact(month, "yyyy-MM-dd", out parsed))
    {
      throw new AppException(Finance.Application.Common.Error.Validation("Invalid month (expected YYYY-MM-01)."));
    }

    var normalized = new DateOnly(parsed.Year, parsed.Month, 1);
    var result = await _mediator.Send(new GetDashboardQuery(normalized), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Ok(result.Value);
  }
}

