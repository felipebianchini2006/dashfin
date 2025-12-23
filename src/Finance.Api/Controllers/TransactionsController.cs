using System.Text.Json.Serialization;
using Finance.Application.Common.Exceptions;
using Finance.Application.Transactions.List;
using Finance.Application.Transactions.Models;
using Finance.Application.Transactions.Update;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("transactions")]
[Authorize]
public sealed class TransactionsController : ControllerBase
{
  private readonly IMediator _mediator;
  public TransactionsController(IMediator mediator) => _mediator = mediator;

  public sealed class ListTransactionsRequest
  {
    [FromQuery(Name = "from")]
    public DateTimeOffset? From { get; init; }

    [FromQuery(Name = "to")]
    public DateTimeOffset? To { get; init; }

    [FromQuery(Name = "account_id")]
    public Guid? AccountId { get; init; }

    [FromQuery(Name = "category_id")]
    public Guid? CategoryId { get; init; }

    [FromQuery(Name = "type")]
    public TransactionFlow? Type { get; init; }

    [FromQuery(Name = "min")]
    public decimal? MinAmount { get; init; }

    [FromQuery(Name = "max")]
    public decimal? MaxAmount { get; init; }

    [FromQuery(Name = "q")]
    public string? Search { get; init; }

    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; init; } = 50;
  }

  public sealed class UpdateTransactionRequest
  {
    [JsonPropertyName("category_id")]
    public Guid? CategoryId { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("ignore_in_dashboard")]
    public bool? IgnoreInDashboard { get; init; }
  }

  [HttpGet]
  public async Task<IActionResult> List([FromQuery] ListTransactionsRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new ListTransactionsQuery(
      request.From,
      request.To,
      request.AccountId,
      request.CategoryId,
      request.Type,
      request.MinAmount,
      request.MaxAmount,
      request.Search,
      request.Page,
      request.PageSize), ct);

    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Ok(result.Value);
  }

  [HttpPatch("{id:guid}")]
  public async Task<IActionResult> Patch([FromRoute] Guid id, [FromBody] UpdateTransactionRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new UpdateTransactionCommand(id, request.CategoryId, request.Notes, request.IgnoreInDashboard), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }
}

