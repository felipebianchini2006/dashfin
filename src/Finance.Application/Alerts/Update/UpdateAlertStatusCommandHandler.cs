using Finance.Application.Abstractions;
using Finance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Alerts.Update;

internal sealed class UpdateAlertStatusCommandHandler : IRequestHandler<UpdateAlertStatusCommand, Result>
{
  private readonly IAppDbContext _db;
  private readonly ICurrentUser _currentUser;

  public UpdateAlertStatusCommandHandler(IAppDbContext db, ICurrentUser currentUser)
  {
    _db = db;
    _currentUser = currentUser;
  }

  public async Task<Result> Handle(UpdateAlertStatusCommand request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail(Error.Unauthorized());

    var alert = await _db.AlertEvents.SingleOrDefaultAsync(a => a.Id == request.AlertId && a.UserId == userId.Value, ct);
    if (alert is null)
      return Result.Fail(Error.NotFound("Alert not found."));

    alert.Status = request.Status;
    await _db.SaveChangesAsync(ct);

    return Result.Ok();
  }
}

