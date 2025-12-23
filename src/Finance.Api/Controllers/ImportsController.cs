using Finance.Application.Common.Exceptions;
using Finance.Application.Imports.Get;
using Finance.Application.Imports.List;
using Finance.Application.Imports.Rows;
using Finance.Application.Imports.Upload;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Route("imports")]
[Authorize]
public sealed class ImportsController : ControllerBase
{
  private readonly IMediator _mediator;
  public ImportsController(IMediator mediator) => _mediator = mediator;

  public sealed class UploadImportRequest
  {
    [FromForm(Name = "account_id")]
    public Guid AccountId { get; init; }

    [FromForm(Name = "pdf")]
    public IFormFile? Pdf { get; init; }
  }

  public sealed record UploadImportResponse(Guid ImportId);

  [HttpGet]
  public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery(Name = "page_size")] int pageSize = 50, CancellationToken ct = default)
  {
    ImportStatus? parsed = null;
    if (!string.IsNullOrWhiteSpace(status))
    {
      if (!Enum.TryParse<ImportStatus>(status, ignoreCase: true, out var s))
        throw new AppException(Finance.Application.Common.Error.Validation("Invalid status."));
      parsed = s;
    }

    var result = await _mediator.Send(new ListImportsQuery(parsed, page, pageSize), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpPost]
  [Consumes("multipart/form-data")]
  public async Task<IActionResult> Upload([FromForm] UploadImportRequest request, CancellationToken ct)
  {
    if (request.Pdf is null)
      throw new AppException(Finance.Application.Common.Error.Validation("Missing PDF file."));

    await using var stream = request.Pdf.OpenReadStream();
    var result = await _mediator.Send(new UploadImportCommand(
      request.AccountId,
      request.Pdf.FileName,
      request.Pdf.ContentType,
      request.Pdf.Length,
      stream), ct);

    if (result.IsFailure)
      throw new AppException(result.Error!);

    return Ok(new UploadImportResponse(result.Value));
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct)
  {
    var result = await _mediator.Send(new GetImportQuery(id), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }

  [HttpGet("{id:guid}/rows")]
  public async Task<IActionResult> ListRows([FromRoute] Guid id, [FromQuery] string? status, CancellationToken ct)
  {
    ImportRowStatus? parsed = null;
    if (!string.IsNullOrWhiteSpace(status))
    {
      if (!Enum.TryParse<ImportRowStatus>(status, ignoreCase: true, out var s))
        throw new AppException(Finance.Application.Common.Error.Validation("Invalid status."));
      parsed = s;
    }

    var result = await _mediator.Send(new ListImportRowsQuery(id, parsed), ct);
    if (result.IsFailure)
      throw new AppException(result.Error!);
    return Ok(result.Value);
  }
}
