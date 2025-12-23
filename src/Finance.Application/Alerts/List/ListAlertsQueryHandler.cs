using Finance.Application.Abstractions;
using Finance.Application.Alerts.Models;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Alerts.List;

internal sealed class ListAlertsQueryHandler : IRequestHandler<ListAlertsQuery, Result<IReadOnlyList<AlertEventDto>>>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public ListAlertsQueryHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result<IReadOnlyList<AlertEventDto>>> Handle(ListAlertsQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<IReadOnlyList<AlertEventDto>>(Error.Unauthorized());

    var query = _db.AlertEvents
      .AsNoTracking()
      .Where(a => a.UserId == userId.Value);

    if (request.Status is not null)
      query = query.Where(a => a.Status == request.Status.Value);

    var items = await query
      .OrderByDescending(a => a.OccurredAt)
      .Select(a => new AlertEventDto(
        a.Id,
        a.AlertRuleId,
        a.Fingerprint,
        a.Status,
        a.OccurredAt,
        a.Title,
        a.Body,
        a.PayloadJson))
      .ToListAsync(ct);

    return Result.Ok<IReadOnlyList<AlertEventDto>>(items);
  }
}

