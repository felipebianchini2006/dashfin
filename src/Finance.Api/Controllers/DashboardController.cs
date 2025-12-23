using Finance.Application.Common.Exceptions;
using Finance.Application.Dashboard.Get;
using Finance.Application.Dashboard.Balances;
using Finance.Application.Dashboard.Categories;
using Finance.Application.Dashboard.Summary;
using Finance.Application.Dashboard.Timeseries;
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

  private static DateOnly ParseMonthOrThrow(string? month)
  {
    if (string.IsNullOrWhiteSpace(month) || !DateOnly.TryParseExact(month, "yyyy-MM-dd", out var parsed))
      throw new AppException(Finance.Application.Common.Error.Validation("Invalid month (expected YYYY-MM-01)."));
    return new DateOnly(parsed.Year, parsed.Month, 1);
  }

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

  [HttpGet("summary")]
  public async Task<IActionResult> Summary([FromQuery(Name = "month")] string? month, CancellationToken ct)
  {
    var normalized = ParseMonthOrThrow(month);
    var result = await _mediator.Send(new GetDashboardSummaryQuery(normalized), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpGet("categories")]
  public async Task<IActionResult> Categories([FromQuery(Name = "month")] string? month, CancellationToken ct)
  {
    var normalized = ParseMonthOrThrow(month);
    var result = await _mediator.Send(new GetDashboardCategoriesQuery(normalized), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpGet("timeseries")]
  public async Task<IActionResult> Timeseries([FromQuery(Name = "month")] string? month, CancellationToken ct)
  {
    var normalized = ParseMonthOrThrow(month);
    var result = await _mediator.Send(new GetDashboardTimeseriesQuery(normalized), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpGet("balances")]
  public async Task<IActionResult> Balances(CancellationToken ct)
  {
    var result = await _mediator.Send(new GetDashboardBalancesQuery(), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }
}
