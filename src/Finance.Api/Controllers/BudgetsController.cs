using System.Text.Json.Serialization;
using Finance.Application.Budgets.Get;
using Finance.Application.Budgets.Upsert;
using Finance.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("budgets")]
[Authorize]
public sealed class BudgetsController : ControllerBase
{
  private readonly IMediator _mediator;
  public BudgetsController(IMediator mediator) => _mediator = mediator;

  public sealed class UpsertBudgetRequest
  {
    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; init; }

    [JsonPropertyName("month")]
    public DateOnly Month { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }
  }

  [HttpGet]
  public async Task<IActionResult> Get([FromQuery(Name = "month")] string? month, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(month) || !DateOnly.TryParseExact(month, "yyyy-MM-dd", out var parsed))
      throw new AppException(Finance.Application.Common.Error.Validation("Invalid month (expected YYYY-MM-01)."));

    var normalized = new DateOnly(parsed.Year, parsed.Month, 1);
    var result = await _mediator.Send(new GetBudgetsQuery(normalized), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Upsert([FromBody] UpsertBudgetRequest request, CancellationToken ct)
  {
    var normalized = new DateOnly(request.Month.Year, request.Month.Month, 1);
    var result = await _mediator.Send(new UpsertBudgetCommand(request.CategoryId, normalized, request.Amount), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }
}

