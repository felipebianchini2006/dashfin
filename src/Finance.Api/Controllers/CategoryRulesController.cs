using System.Text.Json.Serialization;
using Finance.Application.CategoryRules.Create;
using Finance.Application.CategoryRules.List;
using Finance.Application.Common.Exceptions;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("category-rules")]
[Authorize]
public sealed class CategoryRulesController : ControllerBase
{
  private readonly IMediator _mediator;
  public CategoryRulesController(IMediator mediator) => _mediator = mediator;

  public sealed class CreateCategoryRuleRequest
  {
    [JsonPropertyName("pattern")]
    public string Pattern { get; init; } = string.Empty;

    [JsonPropertyName("match_type")]
    public CategoryRuleMatchType MatchType { get; init; }

    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; init; }

    [JsonPropertyName("priority")]
    public int Priority { get; init; } = 100;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; } = true;
  }

  [HttpGet]
  public async Task<IActionResult> List(CancellationToken ct)
  {
    var result = await _mediator.Send(new ListCategoryRulesQuery(), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateCategoryRuleRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new CreateCategoryRuleCommand(
      request.Pattern,
      request.MatchType,
      request.CategoryId,
      request.Priority,
      request.IsActive), ct);

    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Created($"/category-rules/{result.Value!.Id}", result.Value);
  }
}

