using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Imports.Models;
using Finance.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Imports.Get;

internal sealed class GetImportQueryHandler : IRequestHandler<GetImportQuery, Result<ImportDto>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public GetImportQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<ImportDto>> Handle(GetImportQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<ImportDto>(Error.Unauthorized());

    var import = await _db.Imports
      .AsNoTracking()
      .Where(i => i.Id == request.ImportId && i.UserId == userId.Value)
      .Select(i => new
      {
        Import = i,
        Account = i.AccountId == null
          ? null
          : _db.Accounts
            .Where(a => a.Id == i.AccountId && a.UserId == userId.Value)
            .Select(a => new { a.Id, a.Name, a.Type, a.Currency })
            .SingleOrDefault()
      })
      .SingleOrDefaultAsync(ct);

    if (import is null)
      return Result.Fail<ImportDto>(Error.NotFound("Import not found."));

    ImportAccountInfoDto? account = import.Account is null
      ? null
      : new ImportAccountInfoDto(import.Account.Id, import.Account.Name, import.Account.Type, import.Account.Currency);

    return Result.Ok(new ImportDto(
      import.Import.Id,
      import.Import.Status,
      import.Import.SummaryJson,
      import.Import.ErrorMessage,
      import.Import.CreatedAt,
      account));
  }
}

