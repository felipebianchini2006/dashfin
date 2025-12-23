using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Dashboard.Models;
using Finance.Application.Forecasting;
using MediatR;

namespace Finance.Application.Dashboard.Get;

internal sealed class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
  private readonly ICurrentUser _currentUser;
  private readonly ComputeForecastService _forecast;

  public GetDashboardQueryHandler(ICurrentUser currentUser, ComputeForecastService forecast)
  {
    _currentUser = currentUser;
    _forecast = forecast;
  }

  public async Task<Result<DashboardDto>> Handle(GetDashboardQuery request, CancellationToken ct)
  {
    var userId = _currentUser.UserId;
    if (userId is null)
      return Result.Fail<DashboardDto>(Error.Unauthorized());

    var forecast = await _forecast.ComputeAsync(userId.Value, request.Month, ct);
    return Result.Ok(new DashboardDto(forecast));
  }
}

