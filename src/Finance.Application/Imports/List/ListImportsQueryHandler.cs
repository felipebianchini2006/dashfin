using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Imports.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Imports.List;

internal sealed class ListImportsQueryHandler : IRequestHandler<ListImportsQuery, Result<ListImportsResponse>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public ListImportsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<ListImportsResponse>> Handle(ListImportsQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<ListImportsResponse>(Error.Unauthorized());

    var query =
      from i in _db.Imports.AsNoTracking()
      where i.UserId == userId.Value
      join a in _db.Accounts.AsNoTracking().Where(a => a.UserId == userId.Value) on i.AccountId equals a.Id into aj
      from a in aj.DefaultIfEmpty()
      select new { Import = i, Account = a };

    if (request.Status is not null)
      query = query.Where(x => x.Import.Status == request.Status.Value);

    var total = await query.CountAsync(ct);

    var page = request.Page;
    var pageSize = request.PageSize;

    var items = await query
      .OrderByDescending(x => x.Import.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(x => new ImportListItemDto(
        x.Import.Id,
        x.Import.Status,
        x.Import.SummaryJson,
        x.Import.ErrorMessage,
        x.Import.CreatedAt,
        x.Account == null ? null : new ImportAccountInfoDto(x.Account.Id, x.Account.Name, x.Account.Type, x.Account.Currency)))
      .ToListAsync(ct);

    return Result.Ok(new ListImportsResponse(items, page, pageSize, total));
  }
}

