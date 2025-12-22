using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Imports.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Imports.Rows;

internal sealed class ListImportRowsQueryHandler : IRequestHandler<ListImportRowsQuery, Result<IReadOnlyList<ImportRowAuditDto>>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public ListImportRowsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<IReadOnlyList<ImportRowAuditDto>>> Handle(ListImportRowsQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<IReadOnlyList<ImportRowAuditDto>>(Error.Unauthorized());

    var ownsImport = await _db.Imports.AnyAsync(i => i.Id == request.ImportId && i.UserId == userId.Value, ct);
    if (!ownsImport)
      return Result.Fail<IReadOnlyList<ImportRowAuditDto>>(Error.NotFound("Import not found."));

    var rowsQuery = _db.ImportRows
      .AsNoTracking()
      .Where(r => r.ImportId == request.ImportId && r.UserId == userId.Value);

    if (request.Status is not null)
      rowsQuery = rowsQuery.Where(r => r.Status == request.Status.Value);

    var rows = await rowsQuery
      .OrderBy(r => r.RowIndex)
      .Select(r => new ImportRowAuditDto(
        r.Id,
        r.RowIndex,
        r.PageNumber,
        r.Status,
        r.ErrorCode,
        r.ErrorMessage,
        r.CreatedAt))
      .ToListAsync(ct);

    return Result.Ok<IReadOnlyList<ImportRowAuditDto>>(rows);
  }
}

