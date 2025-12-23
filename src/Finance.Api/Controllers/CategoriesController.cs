using System.Text.Json.Serialization;
using Finance.Application.Categories.Create;
using Finance.Application.Categories.List;
using Finance.Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("categories")]
[Authorize]
public sealed class CategoriesController : ControllerBase
{
  private readonly IMediator _mediator;
  public CategoriesController(IMediator mediator) => _mediator = mediator;

  public sealed class CreateCategoryRequest
  {
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("parent_id")]
    public Guid? ParentId { get; init; }
  }

  [HttpGet]
  public async Task<IActionResult> List(CancellationToken ct)
  {
    var result = await _mediator.Send(new ListCategoriesQuery(), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
  {
    var result = await _mediator.Send(new CreateCategoryCommand(request.Name, request.ParentId), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Created($"/categories/{result.Value!.Id}", result.Value);
  }
}

